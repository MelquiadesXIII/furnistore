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

            var total = await orders.CountAsync(cancellationToken);

            var items = await orders
                .OrderByDescending(o => o.OrderDate)
                .ThenBy(o => o.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return Result.Ok(
                new PagedResult<OrderResponse>(items.Select(ToResponse).ToList(), total, query.Page, query.PageSize)
            );
        }

        public async Task<Result<OrderResponse>> GetByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            CancellationToken cancellationToken
        )
        {
            var order = await db
                .Orders.AsNoTracking()
                .Include(o => o.OrderDetails)
                .Where(o => o.Id == id)
                .Where(o => isAdmin || db.Clients.Any(c => c.ID == o.ClientId && c.UserId == userId))
                .FirstOrDefaultAsync(cancellationToken);

            if (order is null)
                return Result.Fail<OrderResponse>(NotFound(id));

            return Result.Ok(ToResponse(order));
        }

        public async Task<Result<OrderResponse>> CreateAsync(
            CreateOrderRequest request,
            string userId,
            bool isAdmin,
            CancellationToken cancellationToken
        )
        {
            var clientId = isAdmin
                ? request.ClientId
                : await GetClientIdAsync(userId, cancellationToken);

            if (clientId is null)
                return Result.Fail<OrderResponse>(
                    Error.Forbidden("order.client_not_owned", "La cuenta no tiene un cliente asociado.")
                );

            var validation = await ValidateReferencesAsync(clientId.Value, request.Lines, cancellationToken);
            if (validation.Error is not null)
                return Result.Fail<OrderResponse>(validation.Error);

            // EnableRetryOnFailure (Program.cs) instala una execution strategy que reintenta operaciones
            // transitoriamente fallidas; una transacción abierta a mano fuera de ExecuteAsync no es
            // compatible con eso (EF Core lanza en tiempo de ejecución), así que toda la transacción
            // vive dentro del delegado para que un reintento la reabra completa.
            var strategy = db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

                var stockError = await ReserveStockAsync(request.Lines, cancellationToken);
                if (stockError is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Fail<OrderResponse>(stockError);
                }

                var orderDetails = request
                    .Lines.Select(l => new OrderDetail
                    {
                        ProductId = l.ProductId,
                        Quantity = l.Quantity,
                        UnitPrice = validation.Prices[l.ProductId],
                    })
                    .ToList();

                var order = new Order
                {
                    ClientId = clientId.Value,
                    OrderDate = DateTime.SpecifyKind(request.OrderDate, DateTimeKind.Utc),
                    DeliveryDate = DateTime.SpecifyKind(request.DeliveryDate, DateTimeKind.Utc),
                    Status = OrderStatus.Pending,
                    Total = orderDetails.Sum(d => d.UnitPrice * d.Quantity),
                    OrderDetails = orderDetails,
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation(
                    ApiEvents.OrderCreated,
                    "Order {OrderId} ({OrderNumber}) created for client {ClientId} with {LineCount} lines by {UserId}",
                    order.Id,
                    order.OrderNumber,
                    order.ClientId,
                    order.OrderDetails.Count,
                    userId
                );

                return Result.Ok(ToResponse(order));
            });
        }

        public async Task<Result> UpdateAsync(
            int id,
            UpdateOrderRequest request,
            string userId,
            bool isAdmin,
            CancellationToken cancellationToken
        )
        {
            var order = await db
                .Orders.Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            if (order is null)
                return Result.Fail(NotFound(id));

            if (!isAdmin && !await IsOwnedByAsync(order.ClientId, userId, cancellationToken))
                return Result.Fail(Error.Forbidden("order.not_owned", "No puedes modificar esta orden."));

            var clientId = isAdmin
                ? request.ClientId
                : await GetClientIdAsync(userId, cancellationToken);
            if (clientId is null)
                return Result.Fail(Error.Forbidden("order.client_not_owned", "La cuenta no tiene un cliente asociado."));

            var validation = await ValidateReferencesAsync(clientId.Value, request.Lines, cancellationToken);
            if (validation.Error is not null)
                return Result.Fail(validation.Error);

            var strategy = db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

                // Se devuelve el stock reservado por las líneas viejas antes de aplicar las nuevas,
                // para no arrastrar el descuento de una línea que ya no existe (o cambió de cantidad).
                await RestoreStockAsync(order.OrderDetails, cancellationToken);

                var stockError = await ReserveStockAsync(request.Lines, cancellationToken);
                if (stockError is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Fail(stockError);
                }

                order.ClientId = clientId.Value;
                order.OrderDate = DateTime.SpecifyKind(request.OrderDate, DateTimeKind.Utc);
                order.DeliveryDate = DateTime.SpecifyKind(request.DeliveryDate, DateTimeKind.Utc);

                db.OrderDetails.RemoveRange(order.OrderDetails);
                var newDetails = request
                    .Lines.Select(l => new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = l.ProductId,
                        Quantity = l.Quantity,
                        UnitPrice = validation.Prices[l.ProductId],
                    })
                    .ToList();
                order.OrderDetails = newDetails;
                order.Total = newDetails.Sum(d => d.UnitPrice * d.Quantity);

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation(
                    ApiEvents.OrderUpdated,
                    "Order {OrderId} updated by {UserId}",
                    id,
                    userId
                );

                return Result.Ok();
            });
        }

        public async Task<Result> DeleteAsync(
            int id,
            string userId,
            bool isAdmin,
            CancellationToken cancellationToken
        )
        {
            var order = await db
                .Orders.Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            if (order is null)
                return Result.Fail(NotFound(id));

            if (!isAdmin && !await IsOwnedByAsync(order.ClientId, userId, cancellationToken))
                return Result.Fail(Error.Forbidden("order.not_owned", "No puedes eliminar esta orden."));

            var strategy = db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

                await RestoreStockAsync(order.OrderDetails, cancellationToken);

                db.OrderDetails.RemoveRange(order.OrderDetails);
                db.Orders.Remove(order);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation(
                    ApiEvents.OrderDeleted,
                    "Order {OrderId} deleted by {UserId}",
                    id,
                    userId
                );

                return Result.Ok();
            });
        }

        private Task<int?> GetClientIdAsync(string userId, CancellationToken cancellationToken) =>
            db.Clients
                .Where(client => client.UserId == userId)
                .Select(client => (int?)client.ID)
                .SingleOrDefaultAsync(cancellationToken);

        private Task<bool> IsOwnedByAsync(
            int clientId,
            string userId,
            CancellationToken cancellationToken
        ) =>
            db.Clients.AnyAsync(
                client => client.ID == clientId && client.UserId == userId,
                cancellationToken
            );

        /// <summary>
        /// Valida cliente/productos y devuelve el precio vigente de cada producto (para snapshotear
        /// en OrderDetail.UnitPrice) junto con un chequeo temprano de stock. La reserva atómica real
        /// contra condiciones de carrera ocurre en <see cref="ReserveStockAsync"/>, dentro de la transacción.
        /// </summary>
        private async Task<(Error? Error, Dictionary<int, decimal> Prices)> ValidateReferencesAsync(
            int clientId,
            IReadOnlyList<OrderLineRequest> lines,
            CancellationToken cancellationToken
        )
        {
            if (!await db.Clients.AnyAsync(c => c.ID == clientId, cancellationToken))
            {
                logger.LogWarning(
                    ApiEvents.OrderRejected,
                    "Order rejected: client {ClientId} does not exist",
                    clientId
                );
                return (Error.Validation("order.client_not_found", $"No existe el cliente {clientId}."), []);
            }

            var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
            var products = await db
                .Products.Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Price, p.Stock })
                .ToListAsync(cancellationToken);

            var missing = productIds.Except(products.Select(p => p.Id)).ToList();
            if (missing.Count > 0)
            {
                logger.LogWarning(
                    ApiEvents.OrderRejected,
                    "Order rejected: products {MissingProductIds} do not exist",
                    string.Join(",", missing)
                );
                return (
                    Error.Validation(
                        "order.product_not_found",
                        $"No existen los productos: {string.Join(", ", missing)}."
                    ),
                    []
                );
            }

            foreach (var line in lines)
            {
                var stock = products.First(p => p.Id == line.ProductId).Stock;
                if (stock < line.Quantity)
                {
                    logger.LogWarning(
                        ApiEvents.OrderRejected,
                        "Order rejected: insufficient stock for product {ProductId} ({Stock} < {Requested})",
                        line.ProductId,
                        stock,
                        line.Quantity
                    );
                    return (
                        Error.Conflict(
                            "order.insufficient_stock",
                            $"Stock insuficiente para el producto {line.ProductId}: quedan {stock}."
                        ),
                        []
                    );
                }
            }

            return (null, products.ToDictionary(p => p.Id, p => p.Price));
        }

        /// <summary>
        /// Descuenta stock de forma atómica (UPDATE condicionado, no lectura-luego-escritura) para que
        /// dos compras concurrentes del mismo producto nunca dejen el stock en negativo. Debe llamarse
        /// dentro de una transacción para que un fallo a mitad de camino revierta las líneas ya aplicadas.
        /// </summary>
        private async Task<Error?> ReserveStockAsync(
            IReadOnlyList<OrderLineRequest> lines,
            CancellationToken cancellationToken
        )
        {
            foreach (var line in lines)
            {
                var affected = await db
                    .Products.Where(p => p.Id == line.ProductId && p.Stock >= line.Quantity)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(p => p.Stock, p => p.Stock - line.Quantity),
                        cancellationToken
                    );

                if (affected == 0)
                {
                    var stock = await db
                        .Products.Where(p => p.Id == line.ProductId)
                        .Select(p => (int?)p.Stock)
                        .FirstOrDefaultAsync(cancellationToken);

                    logger.LogWarning(
                        ApiEvents.OrderRejected,
                        "Order rejected: concurrent stock reservation failed for product {ProductId}",
                        line.ProductId
                    );

                    return Error.Conflict(
                        "order.insufficient_stock",
                        stock is null
                            ? $"El producto {line.ProductId} ya no existe."
                            : $"Stock insuficiente para el producto {line.ProductId}: quedan {stock}."
                    );
                }
            }

            return null;
        }

        private async Task RestoreStockAsync(
            IEnumerable<OrderDetail> details,
            CancellationToken cancellationToken
        )
        {
            foreach (var detail in details)
            {
                await db
                    .Products.Where(p => p.Id == detail.ProductId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(p => p.Stock, p => p.Stock + detail.Quantity),
                        cancellationToken
                    );
            }
        }

        private static OrderResponse ToResponse(Order order) =>
            new(
                order.Id,
                order.OrderNumber,
                order.ClientId,
                order.OrderDate,
                order.DeliveryDate,
                order.Status,
                order.Total,
                order.OrderDetails.Select(d => new OrderLineResponse(d.ProductId, d.Quantity, d.UnitPrice)).ToList()
            );

        private Error NotFound(int id)
        {
            logger.LogWarning(ApiEvents.OrderNotFound, "Order {OrderId} not found", id);
            return Error.NotFound("order.not_found", $"No existe la orden {id}.");
        }
    }
}
