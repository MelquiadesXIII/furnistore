using API.Furnistore.Application.Common;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.Application.Products
{
    public sealed class ProductService(APIFurnistoreContext db, ILogger<ProductService> logger)
    {
        public async Task<Result<PagedResult<ProductResponse>>> SearchAsync(
            ProductQuery query,
            CancellationToken cancellationToken
        )
        {
            var products = db.Products.AsNoTracking();

            if (query.CategoryId is int categoryId)
                products = products.Where(p => p.ProductCategoryId == categoryId);

            if (query.MinPrice is decimal minPrice)
                products = products.Where(p => p.Price >= minPrice);

            if (query.MaxPrice is decimal maxPrice)
                products = products.Where(p => p.Price <= maxPrice);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";
                products = products.Where(p => EF.Functions.ILike(p.Name, pattern, "\\"));
            }

            var total = await products.CountAsync(cancellationToken);

            var items = await products
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(p => new ProductResponse(p.Id, p.Name, p.Price, p.ProductCategoryId, p.ImageUrl))
                .ToListAsync(cancellationToken);

            return Result.Ok(
                new PagedResult<ProductResponse>(items, total, query.Page, query.PageSize)
            );
        }

        public async Task<Result<ProductResponse>> GetByIdAsync(
            int id,
            CancellationToken cancellationToken
        )
        {
            var product = await db
                .Products.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductResponse(p.Id, p.Name, p.Price, p.ProductCategoryId, p.ImageUrl))
                .FirstOrDefaultAsync(cancellationToken);

            if (product is null)
            {
                logger.LogWarning(ApiEvents.ProductNotFound, "Product {ProductId} not found", id);
                return Result.Fail<ProductResponse>(
                    Error.NotFound("product.not_found", $"No existe el producto {id}.")
                );
            }

            return Result.Ok(product);
        }

        public async Task<Result<ProductResponse>> CreateAsync(
            CreateProductRequest request,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var categoryMissing = await CategoryMissingAsync(
                request.ProductCategoryId,
                cancellationToken
            );
            if (categoryMissing is not null)
                return Result.Fail<ProductResponse>(categoryMissing);

            var product = new Product
            {
                Name = request.Name.Trim(),
                Price = request.Price,
                ProductCategoryId = request.ProductCategoryId,
            };

            db.Products.Add(product);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.ProductCreated,
                "Product {ProductId} created in category {CategoryId} by {UserId}",
                product.Id,
                product.ProductCategoryId,
                userId
            );

            return Result.Ok(
                new ProductResponse(
                    product.Id,
                    product.Name,
                    product.Price,
                    product.ProductCategoryId,
                    product.ImageUrl
                )
            );
        }

        public async Task<Result> UpdateAsync(
            int id,
            UpdateProductRequest request,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (product is null)
            {
                logger.LogWarning(ApiEvents.ProductNotFound, "Product {ProductId} not found", id);
                return Result.Fail(
                    Error.NotFound("product.not_found", $"No existe el producto {id}.")
                );
            }

            var categoryMissing = await CategoryMissingAsync(
                request.ProductCategoryId,
                cancellationToken
            );
            if (categoryMissing is not null)
                return Result.Fail(categoryMissing);

            product.Name = request.Name.Trim();
            product.Price = request.Price;
            product.ProductCategoryId = request.ProductCategoryId;

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.ProductUpdated,
                "Product {ProductId} updated by {UserId}",
                product.Id,
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
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (product is null)
            {
                logger.LogWarning(ApiEvents.ProductNotFound, "Product {ProductId} not found", id);
                return Result.Fail(
                    Error.NotFound("product.not_found", $"No existe el producto {id}.")
                );
            }

            var referenced = await db.OrderDetails.AnyAsync(
                od => od.ProductId == id,
                cancellationToken
            );

            if (referenced)
                return Result.Fail(
                    Error.Conflict(
                        "product.referenced_by_order",
                        "No se puede borrar un producto que ya aparece en órdenes."
                    )
                );

            db.Products.Remove(product);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.ProductDeleted,
                "Product {ProductId} deleted by {UserId}",
                id,
                userId
            );

            return Result.Ok();
        }

        private async Task<Error?> CategoryMissingAsync(
            int categoryId,
            CancellationToken cancellationToken
        )
        {
            var exists = await db.ProductCategories.AnyAsync(
                c => c.Id == categoryId,
                cancellationToken
            );

            if (exists)
                return null;

            logger.LogWarning(
                ApiEvents.ProductCategoryMissing,
                "Rejected: product category {CategoryId} does not exist",
                categoryId
            );

            return Error.Validation(
                "product.category_not_found",
                $"La categoría {categoryId} no existe."
            );
        }

        private static string EscapeLikePattern(string value) =>
            value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    }
}
