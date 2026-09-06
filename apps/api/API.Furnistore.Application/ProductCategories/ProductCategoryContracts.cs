using System.ComponentModel.DataAnnotations;

namespace API.Furnistore.Application.ProductCategories
{
    public sealed record ProductCategoryQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 50;

        [StringLength(60)]
        public string? Search { get; init; }
    }

    public sealed record CreateProductCategoryRequest
    {
        [Required, StringLength(60, MinimumLength = 2)]
        public required string Name { get; init; }
    }

    public sealed record UpdateProductCategoryRequest
    {
        [Required, StringLength(60, MinimumLength = 2)]
        public required string Name { get; init; }
    }

    public sealed record ProductCategoryResponse(int Id, string Name);
}
