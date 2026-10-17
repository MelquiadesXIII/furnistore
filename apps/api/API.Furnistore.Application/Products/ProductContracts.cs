using System.ComponentModel.DataAnnotations;
using API.Furnistore.Application.ProductCategories;

namespace API.Furnistore.Application.Products
{
    public sealed record ProductQuery : IValidatableObject
    {
        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 12;

        [StringLength(120)]
        public string? Search { get; init; }

        [Range(1, int.MaxValue)]
        public int? CategoryId { get; init; }

        [Range(0, 1_000_000)]
        public decimal? MinPrice { get; init; }

        [Range(0, 1_000_000)]
        public decimal? MaxPrice { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
                yield return new ValidationResult(
                    "minPrice no puede ser mayor que maxPrice.",
                    new[] { nameof(MinPrice), nameof(MaxPrice) }
                );
        }
    }

    public sealed record ProductResponse(
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
        bool IsActive
    );
}
