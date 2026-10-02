using System.ComponentModel.DataAnnotations;
using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Application.Orders;
using API.Furnistore.Shared;

namespace API.Furnistore.Application.Admin.Orders
{
    public enum AdminOrderAction
    {
        Prepare,
        Ship,
        Deliver,
        Cancel,
    }

    public sealed record AdminOrderQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 20;

        [StringLength(120)]
        public string? Search { get; init; }

        public OrderStatus? Status { get; init; }

        [Range(1, int.MaxValue)]
        public int? CustomerId { get; init; }

        public DateTimeOffset? From { get; init; }

        public DateTimeOffset? To { get; init; }

        [StringLength(40), RegularExpression("^-?[A-Za-z]+$")]
        public string? Sort { get; init; }
    }

    public sealed record AdminCancelOrderRequest
    {
        [StringLength(300)]
        public string? Reason { get; init; }
    }

    public sealed record AdminCustomerRef(int Id, string Name, string Email);

    public sealed record AdminOrderSummary(
        int Id,
        int OrderNumber,
        OrderStatus Status,
        DateTime PlacedAt,
        DateOnly EstimatedDeliveryDate,
        decimal Total,
        int ItemCount,
        AdminCustomerRef Customer
    );

    public sealed record AdminOrderResponse(
        OrderResponse Order,
        AdminCustomerRef Customer,
        IReadOnlyList<AdminOrderAction> AvailableActions,
        IReadOnlyList<AuditEntryResponse> History
    );

    public sealed record AdminOrderStats(
        int Paid,
        int Processing,
        int Shipped,
        int Delivered,
        int Cancelled
    );
}
