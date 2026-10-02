using System.ComponentModel.DataAnnotations;
using API.Furnistore.Application.ProductCategories;

namespace API.Furnistore.Application.Admin.Products
{
    public enum ProductListStatus
    {
        All,
        Active,
        Archived,
    }

    public sealed record AdminProductQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 20;

        [StringLength(120)]
        public string? Search { get; init; }

        [Range(1, int.MaxValue)]
        public int? CategoryId { get; init; }

        public ProductListStatus Status { get; init; } = ProductListStatus.All;

        [Range(0, 1_000_000)]
        public int? MaxStock { get; init; }

        [StringLength(40), RegularExpression("^-?[A-Za-z]+$")]
        public string? Sort { get; init; }
    }

    public record ProductInput
    {
        [Required, StringLength(120, MinimumLength = 2)]
        public required string Name { get; init; }

        [StringLength(2000)]
        public string? Description { get; init; }

        [Range(0.01, 1_000_000)]
        public decimal Price { get; init; }

        [Range(0, 1_000_000)]
        public int Stock { get; init; }

        [Range(1, int.MaxValue)]
        public int ProductCategoryId { get; init; }

        [Url, StringLength(500)]
        public string? ImageUrl { get; init; }

        [Range(1, 1000)]
        public int? WidthCm { get; init; }

        [Range(1, 1000)]
        public int? DepthCm { get; init; }

        [Range(1, 1000)]
        public int? HeightCm { get; init; }

        [StringLength(60)]
        public string? Material { get; init; }

        public bool IsActive { get; init; } = true;
    }

    public sealed record CreateProductRequest : ProductInput;

    public sealed record UpdateProductRequest : ProductInput
    {
        public uint Version { get; init; }
    }

    public sealed record AdminProductResponse(
        int Id,
        string Name,
        string Description,
        decimal Price,
        int Stock,
        ProductCategoryResponse Category,
        string? ImageUrl,
        int? WidthCm,
        int? DepthCm,
        int? HeightCm,
        string? Material,
        bool IsActive,
        DateTime CreatedAt,
        uint Version,
        bool HasOrders
    );
}
