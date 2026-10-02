using System.Linq.Expressions;
using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Application.Common;
using API.Furnistore.Application.ProductCategories;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.Application.Admin.Products
{
    public sealed class AdminProductService(
        APIFurnistoreContext db,
        AuditLog audit,
        ILogger<AdminProductService> logger
    )
    {
        private static readonly SortMap<Product> Sorts = new SortMap<Product>()
            .Add("name", p => p.Name)
            .Add("price", p => p.Price)
            .Add("stock", p => p.Stock)
            .Add("createdAt", p => p.CreatedAt);

        public async Task<Result<PagedResult<AdminProductResponse>>> SearchAsync(
            AdminProductQuery query,
            CancellationToken cancellationToken
        )
        {
            var products = db.Products.AsNoTracking();

            products = query.Status switch
            {
                ProductListStatus.Active => products.Where(p => p.IsActive),
                ProductListStatus.Archived => products.Where(p => !p.IsActive),
                _ => products,
            };

            if (query.CategoryId is int categoryId)
                products = products.Where(p => p.ProductCategoryId == categoryId);

            if (query.MaxStock is int maxStock)
                products = products.Where(p => p.Stock <= maxStock);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = LikePattern.Contains(query.Search);
                products = products.Where(p =>
                    EF.Functions.ILike(p.Name, pattern, LikePattern.Escape)
                    || EF.Functions.ILike(p.Material ?? string.Empty, pattern, LikePattern.Escape)
                );
            }

            var sorted = Sorts.Apply(products, query.Sort, "name");
            if (!sorted.IsSuccess)
                return Result.Fail<PagedResult<AdminProductResponse>>(sorted.Error!);

            var total = await products.CountAsync(cancellationToken);

            var items = await sorted
                .Value.ThenBy(p => p.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(ToResponse())
                .ToListAsync(cancellationToken);

            return Result.Ok(new PagedResult<AdminProductResponse>(items, total, query.Page, query.PageSize));
        }

        public async Task<Result<AdminProductResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var product = await db
                .Products.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(ToResponse())
                .FirstOrDefaultAsync(cancellationToken);

            return product is null
                ? Result.Fail<AdminProductResponse>(NotFound(id))
                : Result.Ok(product);
        }

        public async Task<Result<AdminProductResponse>> CreateAsync(
            CreateProductRequest request,
            string actorUserId,
            CancellationToken cancellationToken
        )
        {
            var category = await FindCategoryAsync(request.ProductCategoryId, cancellationToken);
            if (category is null)
                return Result.Fail<AdminProductResponse>(CategoryMissing(request.ProductCategoryId));

            var product = new Product();
            var changes = Apply(product, request, category);

            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

                db.Products.Add(product);
                await db.SaveChangesAsync(cancellationToken);

                audit.Record(
                    actorUserId,
                    AuditActions.ProductCreated,
                    AuditEntities.Product,
                    product.Id,
                    $"Producto «{product.Name}» creado",
                    changes
                );
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });

            logger.LogInformation(
                ApiEvents.ProductCreated,
                "Product {ProductId} created in category {CategoryId} by {UserId}",
                product.Id,
                product.ProductCategoryId,
                actorUserId
            );

            return await GetByIdAsync(product.Id, cancellationToken);
        }

        public async Task<Result<AdminProductResponse>> UpdateAsync(
            int id,
            UpdateProductRequest request,
            string actorUserId,
            CancellationToken cancellationToken
        )
        {
            var product = await db
                .Products.Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (product is null)
                return Result.Fail<AdminProductResponse>(NotFound(id));

            if (product.Version != request.Version)
                return Result.Fail<AdminProductResponse>(Conflict(id, actorUserId));

            var category = await FindCategoryAsync(request.ProductCategoryId, cancellationToken);
            if (category is null)
                return Result.Fail<AdminProductResponse>(CategoryMissing(request.ProductCategoryId));

            db.Entry(product).Property(p => p.Version).OriginalValue = request.Version;
            var wasActive = product.IsActive;
            var changes = Apply(product, request, category);

            if (changes.IsEmpty)
                return await GetByIdAsync(id, cancellationToken);

            audit.Record(
                actorUserId,
                AuditActions.ProductUpdated,
                AuditEntities.Product,
                product.Id,
                SummaryFor(product.Name, wasActive, product.IsActive),
                changes
            );

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail<AdminProductResponse>(Conflict(id, actorUserId));
            }

            logger.LogInformation(ApiEvents.ProductUpdated, "Product {ProductId} updated by {UserId}", id, actorUserId);

            return await GetByIdAsync(id, cancellationToken);
        }

        public async Task<Result> DeleteAsync(int id, string actorUserId, CancellationToken cancellationToken)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (product is null)
                return Result.Fail(NotFound(id));

            if (await db.OrderDetails.AnyAsync(od => od.ProductId == id, cancellationToken))
                return Result.Fail(
                    Error.Conflict(
                        "product.referenced_by_order",
                        "No se puede borrar un producto que ya aparece en pedidos. Archívalo para ocultarlo del catálogo."
                    )
                );

            db.Products.Remove(product);
            audit.Record(
                actorUserId,
                AuditActions.ProductDeleted,
                AuditEntities.Product,
                id,
                $"Producto «{product.Name}» borrado"
            );
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(ApiEvents.ProductDeleted, "Product {ProductId} deleted by {UserId}", id, actorUserId);

            return Result.Ok();
        }

        private static AuditChanges Apply(Product product, ProductInput input, ProductCategory category)
        {
            var name = input.Name.Trim();
            var description = input.Description?.Trim() ?? string.Empty;
            var imageUrl = TextInput.NullIfBlank(input.ImageUrl);
            var material = TextInput.NullIfBlank(input.Material);

            var changes = new AuditChanges()
                .Track("name", product.Name, name)
                .Track("description", product.Description, description)
                .Track("price", product.Price, input.Price)
                .Track("stock", product.Stock, input.Stock)
                .Track("category", product.Category?.Name, category.Name)
                .Track("imageUrl", product.ImageUrl, imageUrl)
                .Track("widthCm", product.WidthCm, input.WidthCm)
                .Track("depthCm", product.DepthCm, input.DepthCm)
                .Track("heightCm", product.HeightCm, input.HeightCm)
                .Track("material", product.Material, material)
                .Track("isActive", product.IsActive, input.IsActive);

            product.Name = name;
            product.Description = description;
            product.Price = input.Price;
            product.Stock = input.Stock;
            product.Category = category;
            product.ProductCategoryId = category.Id;
            product.ImageUrl = imageUrl;
            product.WidthCm = input.WidthCm;
            product.DepthCm = input.DepthCm;
            product.HeightCm = input.HeightCm;
            product.Material = material;
            product.IsActive = input.IsActive;

            return changes;
        }

        private static string SummaryFor(string name, bool wasActive, bool isActive) =>
            (wasActive, isActive) switch
            {
                (true, false) => $"Producto «{name}» archivado",
                (false, true) => $"Producto «{name}» reactivado",
                _ => $"Producto «{name}» actualizado",
            };

        private Error Conflict(int id, string actorUserId)
        {
            logger.LogWarning(
                ApiEvents.VersionConflict,
                "Product {ProductId} update by {UserId} rejected: stale version",
                id,
                actorUserId
            );
            return AdminErrors.VersionConflict();
        }

        private Expression<Func<Product, AdminProductResponse>> ToResponse() =>
            p => new AdminProductResponse(
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
                p.IsActive,
                p.CreatedAt,
                p.Version,
                db.OrderDetails.Any(od => od.ProductId == p.Id)
            );

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
            return Error.Validation("product.category_not_found", $"La categoría {categoryId} no existe.");
        }
    }
}
