using System.ComponentModel.DataAnnotations;

namespace API.Furnistore.Application.Admin.Categories
{
    public sealed record AdminCategoryQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 50;

        [StringLength(60)]
        public string? Search { get; init; }

        [StringLength(40), RegularExpression("^-?[A-Za-z]+$")]
        public string? Sort { get; init; }
    }

    public sealed record SaveCategoryRequest
    {
        [Required, StringLength(60, MinimumLength = 2)]
        public required string Name { get; init; }
    }

    public sealed record AdminCategoryResponse(int Id, string Name, int ProductCount, int ActiveProductCount);
}
