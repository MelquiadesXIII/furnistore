using API.Furnistore.Application.Common;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.Application.ProductCategories
{
    public sealed class ProductCategoryService(
        APIFurnistoreContext db,
        ILogger<ProductCategoryService> logger
    )
    {
        public async Task<Result<PagedResult<ProductCategoryResponse>>> SearchAsync(
            ProductCategoryQuery query,
            CancellationToken cancellationToken
        )
        {
            var categories = db.ProductCategories.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
                categories = categories.Where(c => EF.Functions.ILike(c.Name, pattern, "\\"));
            }

            var total = await categories.CountAsync(cancellationToken);

            var items = await categories
                .OrderBy(c => c.Name)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(c => new ProductCategoryResponse(c.Id, c.Name))
                .ToListAsync(cancellationToken);

            return Result.Ok(
                new PagedResult<ProductCategoryResponse>(items, total, query.Page, query.PageSize)
            );
        }

        public async Task<Result<ProductCategoryResponse>> GetByIdAsync(
            int id,
            CancellationToken cancellationToken
        )
        {
            var category = await db
                .ProductCategories.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new ProductCategoryResponse(c.Id, c.Name))
                .FirstOrDefaultAsync(cancellationToken);

            if (category is null)
            {
                logger.LogWarning(ApiEvents.CategoryNotFound, "Category {CategoryId} not found", id);
                return Result.Fail<ProductCategoryResponse>(
                    Error.NotFound("category.not_found", $"No existe la categoría {id}.")
                );
            }

            return Result.Ok(category);
        }
    }
}
