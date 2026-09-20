using System.Linq.Expressions;
using API.Furnistore.Application.Clients;
using API.Furnistore.Application.Common;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.Application.Orders
{
    public sealed class OrderService(APIFurnistoreContext db, ILogger<OrderService> logger)
    {
        private const int CheckoutLockNamespace = 3004;

        public async Task<Result<PagedResult<OrderResponse>>> SearchAsync(
            OrderQuery query,
            string userId,
            bool isAdmin,
            CancellationToken cancellationToken
        )
        {
            var orders = db.Orders.AsNoTracking().Include(o => o.OrderDetails).AsQueryable();

            if (!isAdmin)
            {
                var ownerClientId = await GetClientIdAsync(userId, cancellationToken);
                if (ownerClientId is null)
                    return Result.Ok(new PagedResult<OrderResponse>([], 0, query.Page, query.PageSize));

                orders = orders.Where(o => o.ClientId == ownerClientId);
            }

            if (query.ClientId is int clientId)
                orders = orders.Where(o => o.ClientId == clientId);

            if (query.Status is OrderStatus status)
                orders = orders.Where(o => o.Status == status);

            var total = await orders.CountAsync(cancellationToken);

            var items = await orders
                .OrderByDescending(o => o.PlacedAt)
                .ThenByDescending(o => o.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var images = await LoadImagesAsync(items, cancellationToken);

            return Result.Ok(
                new PagedResult<OrderResponse>(
                    items.Select(order => ToResponse(order, images)).ToList(),
                    total,
                    query.Page,
                    query.PageSize
                )
            );
        }

        public async Task<Result<OrderResponse>> GetByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            CancellationToken cancellationToken
        )
        {
            var order = await FindVisibleAsync(id, userId, isAdmin, cancellationToken);

            if (order is null)
                return Result.Fail<OrderResponse>(NotFound(id));

            var images = await LoadImagesAsync([order], cancellationToken);
            return Result.Ok(ToResponse(order, images));
        }

        public async Task<Result<OrderResponse>> CheckoutAsync(
            CheckoutRequest request,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var client = await db
                .Clients.AsNoTracking()
                .Where(c => c.UserId == userId)
                .SingleOrDefaultAsync(cancellationToken);

            if (client is null)
                return Result.Fail<OrderResponse>(
                    CheckoutRejected(
                        Error.NotFound(
                            "checkout.client_not_found",
                            "No se encontró un cliente asociado a esta cuenta."
                        ),
                        userId
                    )
                );

            if (!client.IsProfileComplete)
                return Result.Fail<OrderResponse>(
                    CheckoutRejected(
                        Error.Validation(
                            "checkout.profile_incomplete",
                            "Completa tu teléfono y dirección de envío antes de pagar."
                        ),
                        userId
                    )
                );

            var strategy = db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

                await db.Database.ExecuteSqlAsync(
                    $"SELECT pg_advisory_xact_lock({CheckoutLockNamespace}, {client.ID})",
                    cancellationToken
                );

                var lines = await (
                    from item in db.CartItems
                    join product in db.Products on item.ProductId equals product.Id
                    where item.ClientId == client.ID
                    orderby product.Id
                    select new
                    {
                        product.Id,
                        product.Name,
                        product.Price,
                        product.Stock,
                        product.ImageUrl,
                        product.IsActive,
                        item.Quantity,
                    }
                ).ToListAsync(cancellationToken);

                if (lines.Count == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Fail<OrderResponse>(
                        CheckoutRejected(
                            Error.Validation("checkout.empty_cart", "Tu carrito está vacío."),
                            userId
                        )
                    );
                }

                var archived = lines.FirstOrDefault(line => !line.IsActive);
                if (archived is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Fail<OrderResponse>(
                        CheckoutRejected(
                            Error.Conflict(
                                "checkout.product_unavailable",
                                $"{archived.Name} ya no está disponible. Quítalo de tu carrito para continuar."
                            ),
                            userId
                        )
                    );
                }

                var subtotal = lines.Sum(line => line.Price * line.Quantity);
                var shippingCost = ShippingPolicy.CostFor(subtotal);
                var total = subtotal + shippingCost;

                if (decimal.Round(total, 2) != decimal.Round(request.ExpectedTotal, 2))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Fail<OrderResponse>(
                        CheckoutRejected(
                            Error.Conflict(
                                "checkout.price_changed",
                                "Los precios de tu carrito cambiaron. Revisa el nuevo total antes de confirmar."
                            ),
                            userId
                        )
                    );
                }

                var shortLine = lines.FirstOrDefault(line => line.Stock < line.Quantity);
                var failedProductId =
                    shortLine?.Id
                    ?? await ReserveStockAsync(
                        lines.Select(line => (line.Id, line.Quantity)),
                        cancellationToken
                    );

                if (failedProductId is int productId)
                {
                    var failed = lines.First(line => line.Id == productId);
                    var available = await CurrentStockAsync(productId, cancellationToken) ?? 0;
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Fail<OrderResponse>(
                        CheckoutRejected(
                            Error.Conflict(
                                "checkout.insufficient_stock",
                                $"No hay stock suficiente de {failed.Name}: quedan {available}."
                            ),
                            userId
                        )
                    );
                }

                var now = DateTime.UtcNow;
                var order = new Order
                {
                    ClientId = client.ID,
                    Status = OrderStatus.Paid,
                    Subtotal = subtotal,
                    ShippingCost = shippingCost,
                    Total = total,
                    ShipToName = $"{client.FirstName} {client.LastName}",
                    ShipToPhone = client.Phone!,
                    ShipToStreet = client.Street!,
                    ShipToCity = client.City!,
                    ShipToProvince = client.Province!,
                    ShipToDeliveryNotes = client.DeliveryNotes,
                    PlacedAt = now,
                    PaidAt = now,
                    EstimatedDeliveryDate = DateOnly.FromDateTime(now).AddDays(ShippingPolicy.DeliveryLeadDays),
                    OrderDetails = lines
                        .Select(line => new OrderDetail
                        {
                            ProductId = line.Id,
                            ProductName = line.Name,
                            Quantity = line.Quantity,
                            UnitPrice = line.Price,
                        })
                        .ToList(),
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync(cancellationToken);

                await db.CartItems
                    .Where(item => item.ClientId == client.ID)
                    .ExecuteDeleteAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation(
                    ApiEvents.CheckoutCompleted,
                    "Checkout created order {OrderId} ({OrderNumber}) for client {ClientId}, total {Total}",
                    order.Id,
                    order.OrderNumber,
                    order.ClientId,
                    order.Total
                );

                return Result.Ok(ToResponse(order, lines.ToDictionary(line => line.Id, line => (string?)line.ImageUrl)));
            });
        }

        public async Task<Result<OrderResponse>> CancelAsync(
            int id,
            CancelOrderRequest request,
            string userId,
            bool isAdmin,
            CancellationToken cancellationToken
        )
        {
            var reason = TextInput.NullIfBlank(request.Reason);
            var strategy = db.Database.CreateExecutionStrategy();

            var cancelled = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                var now = DateTime.UtcNow;

                var affected = await Visible(userId, isAdmin)
                    .Where(o => o.Id == id && o.Status == OrderStatus.Paid)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(o => o.Status, OrderStatus.Cancelled)
                            .SetProperty(o => o.CancelledAt, now)
                            .SetProperty(o => o.CancelReason, reason),
                        cancellationToken
                    );

                if (affected == 1)
                {
                    var lines = await db
                        .OrderDetails.Where(d => d.OrderId == id)
                        .OrderBy(d => d.ProductId)
                        .Select(d => new { d.ProductId, d.Quantity })
                        .ToListAsync(cancellationToken);

                    foreach (var line in lines)
                    {
                        await db
                            .Products.Where(p => p.Id == line.ProductId)
                            .ExecuteUpdateAsync(
                                setters => setters.SetProperty(p => p.Stock, p => p.Stock + line.Quantity),
                                cancellationToken
                            );
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                return affected == 1;
            });

            return await TransitionResultAsync(
                id,
                userId,
                isAdmin,
                cancelled,
                "cancelar",
                ApiEvents.OrderCancelled,
                cancellationToken
            );
        }

        public Task<Result<OrderResponse>> ShipAsync(
            int id,
            string userId,
            CancellationToken cancellationToken
        ) =>
            AdvanceAsync(
                id,
                userId,
                OrderStatus.Paid,
                OrderStatus.Shipped,
                o => o.ShippedAt,
                "enviar",
                ApiEvents.OrderShipped,
                cancellationToken
            );

        public Task<Result<OrderResponse>> DeliverAsync(
            int id,
            string userId,
            CancellationToken cancellationToken
        ) =>
            AdvanceAsync(
                id,
                userId,
                OrderStatus.Shipped,
                OrderStatus.Delivered,
                o => o.DeliveredAt,
                "marcar como entregado",
                ApiEvents.OrderDelivered,
                cancellationToken
            );

        private async Task<Result<OrderResponse>> AdvanceAsync(
            int id,
            string userId,
            OrderStatus from,
            OrderStatus to,
            Expression<Func<Order, DateTime?>> stampedAt,
            string action,
            EventId eventId,
            CancellationToken cancellationToken
        )
        {
            var now = DateTime.UtcNow;
            var affected = await db
                .Orders.Where(o => o.Id == id && o.Status == from)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(o => o.Status, to).SetProperty(stampedAt, now),
                    cancellationToken
                );

            return await TransitionResultAsync(
                id,
                userId,
                isAdmin: true,
                affected == 1,
                action,
                eventId,
                cancellationToken
            );
        }

        private async Task<Result<OrderResponse>> TransitionResultAsync(
            int id,
            string userId,
            bool isAdmin,
            bool transitioned,
            string action,
            EventId eventId,
            CancellationToken cancellationToken
        )
        {
            var order = await FindVisibleAsync(id, userId, isAdmin, cancellationToken);

            if (order is null)
                return Result.Fail<OrderResponse>(NotFound(id));

            if (!transitioned)
            {
                logger.LogWarning(
                    ApiEvents.OrderTransitionRejected,
                    "Order {OrderId} rejected '{Action}' while {Status}, requested by {UserId}",
                    id,
                    action,
                    order.Status,
                    userId
                );
                return Result.Fail<OrderResponse>(
                    Error.Conflict(
                        "order.invalid_transition",
                        $"No se puede {action} un pedido {StatusLabel(order.Status)}."
                    )
                );
            }

            logger.LogInformation(
                eventId,
                "Order {OrderId} is now {Status}, by {UserId}",
                id,
                order.Status,
                userId
            );

            var images = await LoadImagesAsync([order], cancellationToken);
            return Result.Ok(ToResponse(order, images));
        }

        private IQueryable<Order> Visible(string userId, bool isAdmin) =>
            db.Orders.Where(o => isAdmin || db.Clients.Any(c => c.ID == o.ClientId && c.UserId == userId));

        private Task<Order?> FindVisibleAsync(
            int id,
            string userId,
            bool isAdmin,
            CancellationToken cancellationToken
        ) =>
            Visible(userId, isAdmin)
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        private Task<int?> GetClientIdAsync(string userId, CancellationToken cancellationToken) =>
            db.Clients
                .Where(client => client.UserId == userId)
                .Select(client => (int?)client.ID)
                .SingleOrDefaultAsync(cancellationToken);

        private Task<int?> CurrentStockAsync(int productId, CancellationToken cancellationToken) =>
            db.Products
                .Where(p => p.Id == productId)
                .Select(p => (int?)p.Stock)
                .FirstOrDefaultAsync(cancellationToken);

        private async Task<int?> ReserveStockAsync(
            IEnumerable<(int ProductId, int Quantity)> lines,
            CancellationToken cancellationToken
        )
        {
            foreach (var line in lines.OrderBy(l => l.ProductId))
            {
                var affected = await db
                    .Products.Where(p => p.Id == line.ProductId && p.Stock >= line.Quantity)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(p => p.Stock, p => p.Stock - line.Quantity),
                        cancellationToken
                    );

                if (affected == 0)
                    return line.ProductId;
            }

            return null;
        }

        private Error CheckoutRejected(Error error, string userId)
        {
            logger.LogWarning(
                ApiEvents.CheckoutRejected,
                "Checkout rejected for {UserId}: {Code}",
                userId,
                error.Code
            );
            return error;
        }

        private async Task<Dictionary<int, string?>> LoadImagesAsync(
            IEnumerable<Order> orders,
            CancellationToken cancellationToken
        )
        {
            var productIds = orders
                .SelectMany(order => order.OrderDetails)
                .Select(detail => detail.ProductId)
                .Distinct()
                .ToList();

            return await db
                .Products.Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.ImageUrl, cancellationToken);
        }

        private static OrderResponse ToResponse(Order order, IReadOnlyDictionary<int, string?> images) =>
            new(
                order.Id,
                order.OrderNumber,
                order.Status,
                order.Status == OrderStatus.Paid,
                order.PlacedAt,
                order.PaidAt,
                order.EstimatedDeliveryDate,
                order.ShippedAt,
                order.DeliveredAt,
                order.CancelledAt,
                order.CancelReason,
                order.Subtotal,
                order.ShippingCost,
                order.Total,
                new OrderShipToResponse(
                    order.ShipToName,
                    order.ShipToPhone,
                    new ShippingAddress
                    {
                        Street = order.ShipToStreet,
                        City = order.ShipToCity,
                        Province = order.ShipToProvince,
                        DeliveryNotes = order.ShipToDeliveryNotes,
                    }
                ),
                order
                    .OrderDetails.OrderBy(d => d.ProductName)
                    .ThenBy(d => d.ProductId)
                    .Select(d => new OrderLineResponse(
                        d.ProductId,
                        d.ProductName,
                        images.GetValueOrDefault(d.ProductId),
                        d.Quantity,
                        d.UnitPrice,
                        d.UnitPrice * d.Quantity
                    ))
                    .ToList()
            );

        private static string StatusLabel(OrderStatus status) =>
            status switch
            {
                OrderStatus.Paid => "pagado",
                OrderStatus.Shipped => "enviado",
                OrderStatus.Delivered => "entregado",
                OrderStatus.Cancelled => "cancelado",
                _ => status.ToString(),
            };

        private Error NotFound(int id)
        {
            logger.LogWarning(ApiEvents.OrderNotFound, "Order {OrderId} not found", id);
            return Error.NotFound("order.not_found", $"No existe la orden {id}.");
        }
    }
}
