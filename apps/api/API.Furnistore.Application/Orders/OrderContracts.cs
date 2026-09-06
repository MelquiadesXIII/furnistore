using System.ComponentModel.DataAnnotations;

namespace API.Furnistore.Application.Orders
{
    public sealed record OrderQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 20;

        [Range(1, int.MaxValue)]
        public int? ClientId { get; init; }
    }

    public sealed record OrderLineRequest
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; init; }

        [Range(1, 10_000)]
        public int Quantity { get; init; }
    }

    public sealed record CreateOrderRequest : IValidatableObject
    {
        [Range(1, int.MaxValue)]
        public int OrderNumber { get; init; }

        [Range(1, int.MaxValue)]
        public int ClientId { get; init; }

        public DateTime OrderDate { get; init; }

        public DateTime DeliveryDate { get; init; }

        [Required, MinLength(1, ErrorMessage = "La orden debe tener al menos una línea.")]
        public required IReadOnlyList<OrderLineRequest> Lines { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            OrderRules.Validate(OrderDate, DeliveryDate, Lines);
    }

    public sealed record UpdateOrderRequest : IValidatableObject
    {
        [Range(1, int.MaxValue)]
        public int OrderNumber { get; init; }

        [Range(1, int.MaxValue)]
        public int ClientId { get; init; }

        public DateTime OrderDate { get; init; }

        public DateTime DeliveryDate { get; init; }

        [Required, MinLength(1, ErrorMessage = "La orden debe tener al menos una línea.")]
        public required IReadOnlyList<OrderLineRequest> Lines { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            OrderRules.Validate(OrderDate, DeliveryDate, Lines);
    }

    internal static class OrderRules
    {
        public static IEnumerable<ValidationResult> Validate(
            DateTime orderDate,
            DateTime deliveryDate,
            IReadOnlyList<OrderLineRequest>? lines
        )
        {
            if (deliveryDate.Date < orderDate.Date)
                yield return new ValidationResult(
                    "deliveryDate no puede ser anterior a orderDate.",
                    new[] { nameof(CreateOrderRequest.DeliveryDate) }
                );

            if (lines is not null && lines.Select(l => l.ProductId).Distinct().Count() != lines.Count)
                yield return new ValidationResult(
                    "No puede repetirse el mismo productId en varias líneas.",
                    new[] { nameof(CreateOrderRequest.Lines) }
                );
        }
    }

    public sealed record OrderLineResponse(int ProductId, int Quantity);

    public sealed record OrderResponse(
        int Id,
        int OrderNumber,
        int ClientId,
        DateTime OrderDate,
        DateTime DeliveryDate,
        IReadOnlyList<OrderLineResponse> Lines
    );
}
