using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Application.Common;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.Application.Orders
{
    public sealed record OrderTransition(
        string Verb,
        IReadOnlyList<OrderStatus> From,
        OrderStatus To,
        string AuditAction,
        EventId Event
    );

    public static class OrderTransitions
    {
        public static readonly OrderTransition Prepare = new(
            "preparar",
            [OrderStatus.Paid],
            OrderStatus.Processing,
            AuditActions.OrderProcessing,
            ApiEvents.OrderProcessing
        );

        public static readonly OrderTransition Ship = new(
            "enviar",
            [OrderStatus.Processing],
            OrderStatus.Shipped,
            AuditActions.OrderShipped,
            ApiEvents.OrderShipped
        );

        public static readonly OrderTransition Deliver = new(
            "marcar como entregado",
            [OrderStatus.Shipped],
            OrderStatus.Delivered,
            AuditActions.OrderDelivered,
            ApiEvents.OrderDelivered
        );

        public static readonly OrderTransition CustomerCancel = new(
            "cancelar",
            [OrderStatus.Paid],
            OrderStatus.Cancelled,
            AuditActions.OrderCancelled,
            ApiEvents.OrderCancelled
        );

        public static readonly OrderTransition AdminCancel = new(
            "cancelar",
            [OrderStatus.Paid, OrderStatus.Processing],
            OrderStatus.Cancelled,
            AuditActions.OrderCancelled,
            ApiEvents.OrderCancelled
        );
    }

    public sealed class OrderWorkflow(
        APIFurnistoreContext db,
        AuditLog audit,
        TimeProvider clock,
        ILogger<OrderWorkflow> logger
    )
    {
        public async Task<Result<Order>> ApplyAsync(
            int orderId,
            int? ownerClientId,
            OrderTransition transition,
            string? reason,
            string actorUserId,
            CancellationToken cancellationToken
        )
        {
            var strategy = db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

                var order = (
                    await db
                        .Orders.FromSql($"SELECT * FROM \"Orders\" WHERE \"Id\" = {orderId} FOR UPDATE")
                        .ToListAsync(cancellationToken)
                ).SingleOrDefault();

                if (order is null || (ownerClientId is int owner && order.ClientId != owner))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    logger.LogWarning(ApiEvents.OrderNotFound, "Order {OrderId} not found", orderId);
                    return Result.Fail<Order>(
                        Error.NotFound("order.not_found", $"No existe la orden {orderId}.")
                    );
                }

                if (!transition.From.Contains(order.Status))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    logger.LogWarning(
                        ApiEvents.OrderTransitionRejected,
                        "Order {OrderId} rejected '{Action}' while {Status}, requested by {UserId}",
                        orderId,
                        transition.Verb,
                        order.Status,
                        actorUserId
                    );
                    return Result.Fail<Order>(
                        Error.Conflict(
                            "order.invalid_transition",
                            $"No se puede {transition.Verb} un pedido {OrderMapping.Label(order.Status)}."
                        )
                    );
                }

                var previous = order.Status;
                var now = clock.GetUtcNow().UtcDateTime;
                var changes = new AuditChanges().Track("status", previous, transition.To);

                order.Status = transition.To;

                switch (transition.To)
                {
                    case OrderStatus.Processing:
                        order.ProcessingAt = now;
                        break;
                    case OrderStatus.Shipped:
                        order.ShippedAt = now;
                        break;
                    case OrderStatus.Delivered:
                        order.DeliveredAt = now;
                        break;
                    case OrderStatus.Cancelled:
                        order.CancelledAt = now;
                        order.CancelReason = TextInput.NullIfBlank(reason);
                        if (order.CancelReason is not null)
                            changes.Set("cancelReason", order.CancelReason);
                        await RestockAsync(order.Id, cancellationToken);
                        break;
                }

                audit.Record(
                    actorUserId,
                    transition.AuditAction,
                    AuditEntities.Order,
                    order.Id,
                    $"Pedido #{order.OrderNumber}: {Capitalize(OrderMapping.Label(previous))} → {Capitalize(OrderMapping.Label(transition.To))}",
                    changes
                );

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation(
                    transition.Event,
                    "Order {OrderId} is now {Status}, by {UserId}",
                    order.Id,
                    order.Status,
                    actorUserId
                );

                await db.Entry(order).Collection(o => o.OrderDetails).LoadAsync(cancellationToken);
                return Result.Ok(order);
            });
        }

        private async Task RestockAsync(int orderId, CancellationToken cancellationToken)
        {
            var lines = await db
                .OrderDetails.Where(d => d.OrderId == orderId)
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

        private static string Capitalize(string label) =>
            string.IsNullOrEmpty(label) ? label : char.ToUpperInvariant(label[0]) + label[1..];
    }
}
