using System.ComponentModel.DataAnnotations;

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

    public sealed record CreateProductRequest
    {
        [Required, StringLength(120, MinimumLength = 2)]
        public required string Name { get; init; }

        [Range(0.01, 1_000_000)]
        public decimal Price { get; init; }

        [Range(1, int.MaxValue)]
        public int ProductCategoryId { get; init; }
    }

    public sealed record UpdateProductRequest
    {
        [Required, StringLength(120, MinimumLength = 2)]
        public required string Name { get; init; }

        [Range(0.01, 1_000_000)]
        public decimal Price { get; init; }

        [Range(1, int.MaxValue)]
        public int ProductCategoryId { get; init; }
    }

    public sealed record ProductResponse(int Id, string Name, decimal Price, int ProductCategoryId, string? ImageUrl);
}
