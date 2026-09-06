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

        public async Task<Result<ProductCategoryResponse>> CreateAsync(
            CreateProductCategoryRequest request,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var name = request.Name.Trim();

            if (await NameTakenAsync(name, null, cancellationToken))
                return Result.Fail<ProductCategoryResponse>(NameTakenError(name));

            var category = new ProductCategory { Name = name };

            db.ProductCategories.Add(category);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.CategoryCreated,
                "Category {CategoryId} created by {UserId}",
                category.Id,
                userId
            );

            return Result.Ok(new ProductCategoryResponse(category.Id, category.Name));
        }

        public async Task<Result> UpdateAsync(
            int id,
            UpdateProductCategoryRequest request,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var category = await db.ProductCategories.FirstOrDefaultAsync(
                c => c.Id == id,
                cancellationToken
            );

            if (category is null)
            {
                logger.LogWarning(ApiEvents.CategoryNotFound, "Category {CategoryId} not found", id);
                return Result.Fail(
                    Error.NotFound("category.not_found", $"No existe la categoría {id}.")
                );
            }

            var name = request.Name.Trim();

            if (await NameTakenAsync(name, id, cancellationToken))
                return Result.Fail(NameTakenError(name));

            category.Name = name;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.CategoryUpdated,
                "Category {CategoryId} updated by {UserId}",
                id,
                userId
            );

            return Result.Ok();
        }

        public async Task<Result> DeleteAsync(
            int id,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var category = await db.ProductCategories.FirstOrDefaultAsync(
                c => c.Id == id,
                cancellationToken
            );

            if (category is null)
            {
                logger.LogWarning(ApiEvents.CategoryNotFound, "Category {CategoryId} not found", id);
                return Result.Fail(
                    Error.NotFound("category.not_found", $"No existe la categoría {id}.")
                );
            }

            var hasProducts = await db.Products.AnyAsync(
                p => p.ProductCategoryId == id,
                cancellationToken
            );

            if (hasProducts)
            {
                logger.LogWarning(
                    ApiEvents.CategoryInUse,
                    "Delete rejected: category {CategoryId} still has products",
                    id
                );
                return Result.Fail(
                    Error.Conflict(
                        "category.has_products",
                        "No se puede borrar una categoría que todavía tiene productos."
                    )
                );
            }

            db.ProductCategories.Remove(category);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.CategoryDeleted,
                "Category {CategoryId} deleted by {UserId}",
                id,
                userId
            );

            return Result.Ok();
        }

        private Task<bool> NameTakenAsync(
            string name,
            int? excludeId,
            CancellationToken cancellationToken
        ) =>
            db.ProductCategories.AnyAsync(
                c => c.Name.ToLower() == name.ToLower() && (excludeId == null || c.Id != excludeId),
                cancellationToken
            );

        private Error NameTakenError(string name)
        {
            logger.LogWarning(ApiEvents.CategoryNameTaken, "Category name {Name} already taken", name);
            return Error.Conflict("category.name_taken", $"Ya existe una categoría llamada «{name}».");
        }
    }
}
