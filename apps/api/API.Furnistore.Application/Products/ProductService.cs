using System.Linq.Expressions;
using API.Furnistore.Application.Common;
using API.Furnistore.Application.ProductCategories;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.Application.Products
{
    public sealed class ProductService(APIFurnistoreContext db, ILogger<ProductService> logger)
    {
        private static readonly Expression<Func<Product, ProductResponse>> ToResponse = p =>
            new ProductResponse(
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Stock,
                new ProductCategoryResponse(p.Category.Id, p.Category.Name),
                p.ImageUrl,
                p.WidthCm,
                p.DepthCm,
                p.HeightCm,
                p.Material,
                p.IsActive
            );

        private static readonly Func<Product, ProductResponse> ToResponseCompiled = ToResponse.Compile();

        public async Task<Result<PagedResult<ProductResponse>>> SearchAsync(
            ProductQuery query,
            bool isAdmin,
            CancellationToken cancellationToken
        )
        {
            var products = db.Products.AsNoTracking();

            if (!(isAdmin && query.IncludeInactive))
                products = products.Where(p => p.IsActive);

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
                .Select(ToResponse)
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
                .Select(ToResponse)
                .FirstOrDefaultAsync(cancellationToken);

            if (product is null)
                return Result.Fail<ProductResponse>(NotFound(id));

            return Result.Ok(product);
        }

        public async Task<Result<ProductResponse>> CreateAsync(
            CreateProductRequest request,
            string userId,
            CancellationToken cancellationToken
        )
        {
            var category = await FindCategoryAsync(request.ProductCategoryId, cancellationToken);
            if (category is null)
                return Result.Fail<ProductResponse>(CategoryMissing(request.ProductCategoryId));

            var product = new Product
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                Price = request.Price,
                Stock = request.Stock,
                Category = category,
                ImageUrl = TextInput.NullIfBlank(request.ImageUrl),
                WidthCm = request.WidthCm,
                DepthCm = request.DepthCm,
                HeightCm = request.HeightCm,
                Material = TextInput.NullIfBlank(request.Material),
                IsActive = request.IsActive,
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

            return Result.Ok(ToResponseCompiled(product));
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
                return Result.Fail(NotFound(id));

            var category = await FindCategoryAsync(request.ProductCategoryId, cancellationToken);
            if (category is null)
                return Result.Fail(CategoryMissing(request.ProductCategoryId));

            product.Name = request.Name.Trim();
            product.Description = request.Description?.Trim() ?? string.Empty;
            product.Price = request.Price;
            product.Stock = request.Stock;
            product.ProductCategoryId = category.Id;
            product.ImageUrl = TextInput.NullIfBlank(request.ImageUrl);
            product.WidthCm = request.WidthCm;
            product.DepthCm = request.DepthCm;
            product.HeightCm = request.HeightCm;
            product.Material = TextInput.NullIfBlank(request.Material);
            product.IsActive = request.IsActive;

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
                return Result.Fail(NotFound(id));

            var referenced = await db.OrderDetails.AnyAsync(
                od => od.ProductId == id,
                cancellationToken
            );

            if (referenced)
                return Result.Fail(
                    Error.Conflict(
                        "product.referenced_by_order",
                        "No se puede borrar un producto que ya aparece en órdenes. Archívalo para ocultarlo del catálogo."
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

        private Task<ProductCategory?> FindCategoryAsync(int categoryId, CancellationToken cancellationToken) =>
            db.ProductCategories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        private Error NotFound(int id)
        {
            logger.LogWarning(ApiEvents.ProductNotFound, "Product {ProductId} not found", id);
            return Error.NotFound("product.not_found", $"No existe el producto {id}.");
        }

        private Error CategoryMissing(int categoryId)
        {
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
