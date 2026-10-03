using API.Furnistore.Application.Common;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.Application.Orders
{
    public sealed class OrderService(
        APIFurnistoreContext db,
        OrderWorkflow workflow,
        ILogger<OrderService> logger
    )
    {
        private const int CheckoutLockNamespace = 3004;

        public async Task<Result<PagedResult<OrderResponse>>> SearchAsync(
            OrderQuery query,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var clientId = await GetClientIdAsync(userId, cancellationToken);
            if (clientId is null)
                return Result.Ok(new PagedResult<OrderResponse>([], 0, query.Page, query.PageSize));

            var orders = db
                .Orders.AsNoTracking()
                .Include(o => o.OrderDetails)
                .Where(o => o.ClientId == clientId);

            if (query.Status is OrderStatus status)
                orders = orders.Where(o => o.Status == status);

            var total = await orders.CountAsync(cancellationToken);

            var items = await orders
                .OrderByDescending(o => o.PlacedAt)
                .ThenByDescending(o => o.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var images = await OrderMapping.LoadImagesAsync(db, items, cancellationToken);

            return Result.Ok(
                new PagedResult<OrderResponse>(
                    items.Select(order => OrderMapping.ToResponse(order, images)).ToList(),
                    total,
                    query.Page,
                    query.PageSize
                )
            );
        }

        public async Task<Result<OrderResponse>> GetByIdAsync(
            int id,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var clientId = await GetClientIdAsync(userId, cancellationToken);

            var order = clientId is null
                ? null
                : await db
                    .Orders.AsNoTracking()
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.Id == id && o.ClientId == clientId, cancellationToken);

            if (order is null)
                return Result.Fail<OrderResponse>(NotFound(id));

            var images = await OrderMapping.LoadImagesAsync(db, [order], cancellationToken);
            return Result.Ok(OrderMapping.ToResponse(order, images));
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

                return Result.Ok(OrderMapping.ToResponse(order, lines.ToDictionary(line => line.Id, line => (string?)line.ImageUrl)));
            });
        }

        public async Task<Result<OrderResponse>> CancelAsync(
            int id,
            CancelOrderRequest request,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var clientId = await GetClientIdAsync(userId, cancellationToken);
            if (clientId is null)
                return Result.Fail<OrderResponse>(NotFound(id));

            var result = await workflow.ApplyAsync(
                id,
                clientId,
                OrderTransitions.CustomerCancel,
                request.Reason,
                userId,
                cancellationToken
            );

            if (!result.IsSuccess)
                return Result.Fail<OrderResponse>(result.Error!);

            var images = await OrderMapping.LoadImagesAsync(db, [result.Value], cancellationToken);
            return Result.Ok(OrderMapping.ToResponse(result.Value, images));
        }

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

        private Error NotFound(int id)
        {
            logger.LogWarning(ApiEvents.OrderNotFound, "Order {OrderId} not found", id);
            return Error.NotFound("order.not_found", $"No existe la orden {id}.");
        }
    }
}
