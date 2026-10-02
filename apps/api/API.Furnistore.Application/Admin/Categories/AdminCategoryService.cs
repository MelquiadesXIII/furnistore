using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Application.Common;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.Application.Admin.Categories
{
    public sealed class AdminCategoryService(
        APIFurnistoreContext db,
        AuditLog audit,
        ILogger<AdminCategoryService> logger
    )
    {
        private static readonly SortMap<CategoryRow> Sorts = new SortMap<CategoryRow>()
            .Add("name", c => c.Name)
            .Add("productCount", c => c.ProductCount);

        public async Task<Result<PagedResult<AdminCategoryResponse>>> SearchAsync(
            AdminCategoryQuery query,
            CancellationToken cancellationToken
        )
        {
            var categories = db.ProductCategories.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = LikePattern.Contains(query.Search);
                categories = categories.Where(c => EF.Functions.ILike(c.Name, pattern, LikePattern.Escape));
            }

            var rows = categories.Select(c => new CategoryRow
            {
                Id = c.Id,
                Name = c.Name,
                ProductCount = db.Products.Count(p => p.ProductCategoryId == c.Id),
                ActiveProductCount = db.Products.Count(p => p.ProductCategoryId == c.Id && p.IsActive),
            });

            var sorted = Sorts.Apply(rows, query.Sort, "name");
            if (!sorted.IsSuccess)
                return Result.Fail<PagedResult<AdminCategoryResponse>>(sorted.Error!);

            var total = await categories.CountAsync(cancellationToken);

            var items = await sorted
                .Value.ThenBy(c => c.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(c => new AdminCategoryResponse(c.Id, c.Name, c.ProductCount, c.ActiveProductCount))
                .ToListAsync(cancellationToken);

            return Result.Ok(new PagedResult<AdminCategoryResponse>(items, total, query.Page, query.PageSize));
        }

        public async Task<Result<AdminCategoryResponse>> CreateAsync(
            SaveCategoryRequest request,
            string actorUserId,
            CancellationToken cancellationToken
        )
        {
            var name = request.Name.Trim();

            if (await NameTakenAsync(name, null, cancellationToken))
                return Result.Fail<AdminCategoryResponse>(NameTaken(name));

            var category = new ProductCategory { Name = name };

            try
            {
                await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                    db.ProductCategories.Add(category);
                    await db.SaveChangesAsync(cancellationToken);
                    audit.Record(
                        actorUserId,
                        AuditActions.CategoryCreated,
                        AuditEntities.Category,
                        category.Id,
                        $"Categoría «{name}» creada"
                    );
                    await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                });
            }
            catch (DbUpdateException exception) when (AdminErrors.IsUniqueViolation(exception))
            {
                return Result.Fail<AdminCategoryResponse>(NameTaken(name));
            }

            logger.LogInformation(ApiEvents.CategoryCreated, "Category {CategoryId} created by {UserId}", category.Id, actorUserId);

            return Result.Ok(new AdminCategoryResponse(category.Id, category.Name, 0, 0));
        }

        public async Task<Result<AdminCategoryResponse>> UpdateAsync(
            int id,
            SaveCategoryRequest request,
            string actorUserId,
            CancellationToken cancellationToken
        )
        {
            var category = await db.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (category is null)
                return Result.Fail<AdminCategoryResponse>(NotFound(id));

            var name = request.Name.Trim();

            if (await NameTakenAsync(name, id, cancellationToken))
                return Result.Fail<AdminCategoryResponse>(NameTaken(name));

            var changes = new AuditChanges().Track("name", category.Name, name);

            if (!changes.IsEmpty)
            {
                audit.Record(
                    actorUserId,
                    AuditActions.CategoryUpdated,
                    AuditEntities.Category,
                    id,
                    $"Categoría «{category.Name}» renombrada a «{name}»",
                    changes
                );
                category.Name = name;

                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException exception) when (AdminErrors.IsUniqueViolation(exception))
                {
                    return Result.Fail<AdminCategoryResponse>(NameTaken(name));
                }

                logger.LogInformation(ApiEvents.CategoryUpdated, "Category {CategoryId} updated by {UserId}", id, actorUserId);
            }

            var counts = await db
                .Products.Where(p => p.ProductCategoryId == id)
                .GroupBy(p => 1)
                .Select(g => new { Total = g.Count(), Active = g.Count(p => p.IsActive) })
                .FirstOrDefaultAsync(cancellationToken);

            return Result.Ok(new AdminCategoryResponse(id, category.Name, counts?.Total ?? 0, counts?.Active ?? 0));
        }

        public async Task<Result> DeleteAsync(int id, string actorUserId, CancellationToken cancellationToken)
        {
            var category = await db.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (category is null)
                return Result.Fail(NotFound(id));

            if (await db.Products.AnyAsync(p => p.ProductCategoryId == id, cancellationToken))
            {
                logger.LogWarning(ApiEvents.CategoryInUse, "Delete rejected: category {CategoryId} still has products", id);
                return Result.Fail(
                    Error.Conflict(
                        "category.has_products",
                        "No se puede borrar una categoría que todavía tiene productos."
                    )
                );
            }

            db.ProductCategories.Remove(category);
            audit.Record(
                actorUserId,
                AuditActions.CategoryDeleted,
                AuditEntities.Category,
                id,
                $"Categoría «{category.Name}» borrada"
            );
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(ApiEvents.CategoryDeleted, "Category {CategoryId} deleted by {UserId}", id, actorUserId);

            return Result.Ok();
        }

        private Task<bool> NameTakenAsync(string name, int? excludeId, CancellationToken cancellationToken) =>
            db.ProductCategories.AnyAsync(
                c => c.Name.ToLower() == name.ToLower() && (excludeId == null || c.Id != excludeId),
                cancellationToken
            );

        private Error NotFound(int id)
        {
            logger.LogWarning(ApiEvents.CategoryNotFound, "Category {CategoryId} not found", id);
            return Error.NotFound("category.not_found", $"No existe la categoría {id}.");
        }

        private Error NameTaken(string name)
        {
            logger.LogWarning(ApiEvents.CategoryNameTaken, "Category name {Name} already taken", name);
            return Error.Conflict("category.name_taken", $"Ya existe una categoría llamada «{name}».");
        }

        private sealed class CategoryRow
        {
            public required int Id { get; init; }

            public required string Name { get; init; }

            public required int ProductCount { get; init; }

            public required int ActiveProductCount { get; init; }
        }
    }
}
