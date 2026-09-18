using System.ComponentModel.DataAnnotations;

namespace API.Furnistore.Application.Carts
{
    public sealed record AddCartItemRequest
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; init; }

        [Range(1, 10_000)]
        public int Quantity { get; init; } = 1;
    }

    public sealed record UpdateCartItemRequest
    {
        [Range(1, 10_000)]
        public int Quantity { get; init; }
    }

    public sealed record CartItemResponse(
        int ProductId,
        string ProductName,
        string? ImageUrl,
        decimal UnitPrice,
        int Stock,
        int Quantity
    );

    public sealed record CartResponse(IReadOnlyList<CartItemResponse> Items, decimal Subtotal);
}
