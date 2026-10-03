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

        public async Task<Result<PagedResult<ProductResponse>>> SearchAsync(
            ProductQuery query,
            CancellationToken cancellationToken
        )
        {
            var products = db.Products.AsNoTracking().Where(p => p.IsActive);

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

        private Error NotFound(int id)
        {
            logger.LogWarning(ApiEvents.ProductNotFound, "Product {ProductId} not found", id);
            return Error.NotFound("product.not_found", $"No existe el producto {id}.");
        }

        private static string EscapeLikePattern(string value) =>
            value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    }
}
