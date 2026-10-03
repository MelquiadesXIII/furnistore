using System.ComponentModel.DataAnnotations;
using API.Furnistore.Application.Clients;
using API.Furnistore.Shared;

namespace API.Furnistore.Application.Orders
{
    public sealed record OrderQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 20;

        public OrderStatus? Status { get; init; }
    }

    public sealed record CheckoutRequest
    {
        [Range(typeof(decimal), "0", "100000000")]
        public decimal ExpectedTotal { get; init; }
    }

    public sealed record CancelOrderRequest
    {
        [StringLength(300)]
        public string? Reason { get; init; }
    }

    public sealed record OrderLineResponse(
        int ProductId,
        string ProductName,
        string? ImageUrl,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal
    );

    public sealed record OrderShipToResponse(string Name, string Phone, ShippingAddress Address);

    public sealed record OrderResponse(
        int Id,
        int OrderNumber,
        OrderStatus Status,
        bool CanCancel,
        DateTime PlacedAt,
        DateTime? PaidAt,
        DateOnly EstimatedDeliveryDate,
        DateTime? ProcessingAt,
        DateTime? ShippedAt,
        DateTime? DeliveredAt,
        DateTime? CancelledAt,
        string? CancelReason,
        decimal Subtotal,
        decimal ShippingCost,
        decimal Total,
        OrderShipToResponse ShipTo,
        IReadOnlyList<OrderLineResponse> Lines
    );
}
