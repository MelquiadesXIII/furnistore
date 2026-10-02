using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Application.Common;
using API.Furnistore.Application.Orders;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.Application.Admin.Orders
{
    public sealed class AdminOrderService(
        APIFurnistoreContext db,
        OrderWorkflow workflow,
        AuditService audit,
        ILogger<AdminOrderService> logger
    )
    {
        private static readonly (AdminOrderAction Action, OrderTransition Transition)[] Actions =
        [
            (AdminOrderAction.Prepare, OrderTransitions.Prepare),
            (AdminOrderAction.Ship, OrderTransitions.Ship),
            (AdminOrderAction.Deliver, OrderTransitions.Deliver),
            (AdminOrderAction.Cancel, OrderTransitions.AdminCancel),
        ];

        private static readonly SortMap<OrderRow> Sorts = new SortMap<OrderRow>()
            .Add("placedAt", row => row.Order.PlacedAt)
            .Add("orderNumber", row => row.Order.OrderNumber)
            .Add("total", row => row.Order.Total)
            .Add("customer", row => row.LastName);

        public async Task<Result<PagedResult<AdminOrderSummary>>> SearchAsync(
            AdminOrderQuery query,
            CancellationToken cancellationToken
        )
        {
            var rows = Rows();

            if (query.Status is OrderStatus status)
                rows = rows.Where(row => row.Order.Status == status);

            if (query.CustomerId is int customerId)
                rows = rows.Where(row => row.Order.ClientId == customerId);

            if (query.From is DateTimeOffset from)
                rows = rows.Where(row => row.Order.PlacedAt >= from.UtcDateTime);

            if (query.To is DateTimeOffset to)
                rows = rows.Where(row => row.Order.PlacedAt < to.UtcDateTime);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().TrimStart('#');
                var pattern = LikePattern.Contains(term);

                rows = int.TryParse(term, out var orderNumber)
                    ? rows.Where(row => row.Order.OrderNumber == orderNumber)
                    : rows.Where(row =>
                        EF.Functions.ILike(row.FirstName + " " + row.LastName, pattern, LikePattern.Escape)
                        || EF.Functions.ILike(row.Email, pattern, LikePattern.Escape)
                    );
            }

            var sorted = Sorts.Apply(rows, query.Sort, "-placedAt");
            if (!sorted.IsSuccess)
                return Result.Fail<PagedResult<AdminOrderSummary>>(sorted.Error!);

            var total = await rows.CountAsync(cancellationToken);

            var items = await sorted
                .Value.ThenByDescending(row => row.Order.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(row => new AdminOrderSummary(
                    row.Order.Id,
                    row.Order.OrderNumber,
                    row.Order.Status,
                    row.Order.PlacedAt,
                    row.Order.EstimatedDeliveryDate,
                    row.Order.Total,
                    row.Order.OrderDetails.Sum(d => d.Quantity),
                    new AdminCustomerRef(row.Order.ClientId, row.FirstName + " " + row.LastName, row.Email)
                ))
                .ToListAsync(cancellationToken);

            return Result.Ok(new PagedResult<AdminOrderSummary>(items, total, query.Page, query.PageSize));
        }

        public async Task<Result<AdminOrderStats>> StatsAsync(CancellationToken cancellationToken)
        {
            var counts = await db
                .Orders.AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken);

            return Result.Ok(
                new AdminOrderStats(
                    counts.GetValueOrDefault(OrderStatus.Paid),
                    counts.GetValueOrDefault(OrderStatus.Processing),
                    counts.GetValueOrDefault(OrderStatus.Shipped),
                    counts.GetValueOrDefault(OrderStatus.Delivered),
                    counts.GetValueOrDefault(OrderStatus.Cancelled)
                )
            );
        }

        public async Task<Result<AdminOrderResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var order = await db
                .Orders.AsNoTracking()
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            if (order is null)
                return Result.Fail<AdminOrderResponse>(NotFound(id));

            return Result.Ok(await ToResponseAsync(order, cancellationToken));
        }

        public Task<Result<AdminOrderResponse>> PrepareAsync(int id, string actorUserId, CancellationToken cancellationToken) =>
            MoveAsync(id, OrderTransitions.Prepare, null, actorUserId, cancellationToken);

        public Task<Result<AdminOrderResponse>> ShipAsync(int id, string actorUserId, CancellationToken cancellationToken) =>
            MoveAsync(id, OrderTransitions.Ship, null, actorUserId, cancellationToken);

        public Task<Result<AdminOrderResponse>> DeliverAsync(int id, string actorUserId, CancellationToken cancellationToken) =>
            MoveAsync(id, OrderTransitions.Deliver, null, actorUserId, cancellationToken);

        public Task<Result<AdminOrderResponse>> CancelAsync(
            int id,
            AdminCancelOrderRequest request,
            string actorUserId,
            CancellationToken cancellationToken
        ) => MoveAsync(id, OrderTransitions.AdminCancel, request.Reason, actorUserId, cancellationToken);

        private async Task<Result<AdminOrderResponse>> MoveAsync(
            int id,
            OrderTransition transition,
            string? reason,
            string actorUserId,
            CancellationToken cancellationToken
        )
        {
            var result = await workflow.ApplyAsync(id, null, transition, reason, actorUserId, cancellationToken);

            return result.IsSuccess
                ? Result.Ok(await ToResponseAsync(result.Value, cancellationToken))
                : Result.Fail<AdminOrderResponse>(result.Error!);
        }

        private async Task<AdminOrderResponse> ToResponseAsync(Order order, CancellationToken cancellationToken)
        {
            var customer = await (
                from client in db.Clients.AsNoTracking()
                join user in db.Users on client.UserId equals user.Id
                where client.ID == order.ClientId
                select new AdminCustomerRef(client.ID, client.FirstName + " " + client.LastName, user.Email ?? string.Empty)
            ).SingleAsync(cancellationToken);

            var images = await OrderMapping.LoadImagesAsync(db, [order], cancellationToken);
            var history = await audit.ForEntityAsync(AuditEntities.Order, order.Id, cancellationToken);

            return new AdminOrderResponse(
                OrderMapping.ToResponse(order, images),
                customer,
                Actions.Where(a => a.Transition.From.Contains(order.Status)).Select(a => a.Action).ToList(),
                history
            );
        }

        private IQueryable<OrderRow> Rows() =>
            from order in db.Orders.AsNoTracking()
            join client in db.Clients on order.ClientId equals client.ID
            join user in db.Users on client.UserId equals user.Id
            select new OrderRow
            {
                Order = order,
                FirstName = client.FirstName,
                LastName = client.LastName,
                Email = user.Email ?? string.Empty,
            };

        private Error NotFound(int id)
        {
            logger.LogWarning(ApiEvents.OrderNotFound, "Order {OrderId} not found", id);
            return Error.NotFound("order.not_found", $"No existe la orden {id}.");
        }

        private sealed class OrderRow
        {
            public required Order Order { get; init; }

            public required string FirstName { get; init; }

            public required string LastName { get; init; }

            public required string Email { get; init; }
        }
    }
}
