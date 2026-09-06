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
            CancellationToken cancellationToken
        )
        {
            var orders = db.Orders.AsNoTracking();

            if (query.ClientId is int clientId)
                orders = orders.Where(o => o.ClientId == clientId);

            var total = await orders.CountAsync(cancellationToken);

            var items = await orders
                .OrderByDescending(o => o.OrderDate)
                .ThenBy(o => o.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(o => new OrderResponse(
                    o.Id,
                    o.OrderNumber,
                    o.ClientId,
                    o.OrderDate,
                    o.DeliveryDate,
                    o.OrderDetails.Select(d => new OrderLineResponse(d.ProductId, d.Quantity)).ToList()
                ))
                .ToListAsync(cancellationToken);

            return Result.Ok(new PagedResult<OrderResponse>(items, total, query.Page, query.PageSize));
        }

        public async Task<Result<OrderResponse>> GetByIdAsync(
            int id,
            CancellationToken cancellationToken
        )
        {
            var order = await db
                .Orders.AsNoTracking()
                .Where(o => o.Id == id)
                .Select(o => new OrderResponse(
                    o.Id,
                    o.OrderNumber,
                    o.ClientId,
                    o.OrderDate,
                    o.DeliveryDate,
                    o.OrderDetails.Select(d => new OrderLineResponse(d.ProductId, d.Quantity)).ToList()
                ))
                .FirstOrDefaultAsync(cancellationToken);

            if (order is null)
                return Result.Fail<OrderResponse>(NotFound(id));

            return Result.Ok(order);
        }

        public async Task<Result<OrderResponse>> CreateAsync(
            CreateOrderRequest request,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var referenceError = await ValidateReferencesAsync(
                request.ClientId,
                request.Lines,
                cancellationToken
            );
            if (referenceError is not null)
                return Result.Fail<OrderResponse>(referenceError);

            var order = new Order
            {
                OrderNumber = request.OrderNumber,
                ClientId = request.ClientId,
                OrderDate = DateTime.SpecifyKind(request.OrderDate, DateTimeKind.Utc),
                DeliveryDate = DateTime.SpecifyKind(request.DeliveryDate, DateTimeKind.Utc),
                OrderDetails = request
                    .Lines.Select(l => new OrderDetail
                    {
                        ProductId = l.ProductId,
                        Quantity = l.Quantity,
                    })
                    .ToList(),
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.OrderCreated,
                "Order {OrderId} created for client {ClientId} with {LineCount} lines by {UserId}",
                order.Id,
                order.ClientId,
                order.OrderDetails.Count,
                userId
            );

            return Result.Ok(
                new OrderResponse(
                    order.Id,
                    order.OrderNumber,
                    order.ClientId,
                    order.OrderDate,
                    order.DeliveryDate,
                    order.OrderDetails.Select(d => new OrderLineResponse(d.ProductId, d.Quantity)).ToList()
                )
            );
        }

        public async Task<Result> UpdateAsync(
            int id,
            UpdateOrderRequest request,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var order = await db
                .Orders.Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            if (order is null)
                return Result.Fail(NotFound(id));

            var referenceError = await ValidateReferencesAsync(
                request.ClientId,
                request.Lines,
                cancellationToken
            );
            if (referenceError is not null)
                return Result.Fail(referenceError);

            order.OrderNumber = request.OrderNumber;
            order.ClientId = request.ClientId;
            order.OrderDate = DateTime.SpecifyKind(request.OrderDate, DateTimeKind.Utc);
            order.DeliveryDate = DateTime.SpecifyKind(request.DeliveryDate, DateTimeKind.Utc);

            db.OrderDetails.RemoveRange(order.OrderDetails);
            order.OrderDetails = request
                .Lines.Select(l => new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                })
                .ToList();

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.OrderUpdated,
                "Order {OrderId} updated by {UserId}",
                id,
                userId
            );

            return Result.Ok();
        }

        public async Task<Result> DeleteAsync(
            int id,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var order = await db
                .Orders.Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            if (order is null)
                return Result.Fail(NotFound(id));

            db.OrderDetails.RemoveRange(order.OrderDetails);
            db.Orders.Remove(order);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.OrderDeleted,
                "Order {OrderId} deleted by {UserId}",
                id,
                userId
            );

            return Result.Ok();
        }

        private async Task<Error?> ValidateReferencesAsync(
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
                return Error.Validation("order.client_not_found", $"No existe el cliente {clientId}.");
            }

            var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
            var existing = await db
                .Products.Where(p => productIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            var missing = productIds.Except(existing).ToList();

            if (missing.Count > 0)
            {
                logger.LogWarning(
                    ApiEvents.OrderRejected,
                    "Order rejected: products {MissingProductIds} do not exist",
                    string.Join(",", missing)
                );
                return Error.Validation(
                    "order.product_not_found",
                    $"No existen los productos: {string.Join(", ", missing)}."
                );
            }

            return null;
        }

        private Error NotFound(int id)
        {
            logger.LogWarning(ApiEvents.OrderNotFound, "Order {OrderId} not found", id);
            return Error.NotFound("order.not_found", $"No existe la orden {id}.");
        }
    }
}
