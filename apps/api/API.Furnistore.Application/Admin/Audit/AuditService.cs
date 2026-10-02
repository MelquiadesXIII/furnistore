using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace API.Furnistore.Application.Admin.Audit
{
    public sealed class AuditService(APIFurnistoreContext db)
    {
        public async Task<Result<PagedResult<AuditEntryResponse>>> SearchAsync(
            AuditQuery query,
            CancellationToken cancellationToken
        )
        {
            var entries = db.AuditEntries.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.EntityType))
                entries = entries.Where(e => e.EntityType == query.EntityType.Trim());

            if (!string.IsNullOrWhiteSpace(query.EntityId))
                entries = entries.Where(e => e.EntityId == query.EntityId.Trim());

            if (!string.IsNullOrWhiteSpace(query.Action))
                entries = entries.Where(e => e.Action == query.Action.Trim());

            if (!string.IsNullOrWhiteSpace(query.ActorUserId))
                entries = entries.Where(e => e.ActorUserId == query.ActorUserId.Trim());

            if (query.From is DateTimeOffset from)
                entries = entries.Where(e => e.OccurredAt >= from.UtcDateTime);

            if (query.To is DateTimeOffset to)
                entries = entries.Where(e => e.OccurredAt < to.UtcDateTime);

            var total = await entries.CountAsync(cancellationToken);

            var page = await entries
                .OrderByDescending(e => e.OccurredAt)
                .ThenByDescending(e => e.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return Result.Ok(
                new PagedResult<AuditEntryResponse>(
                    await ToResponsesAsync(page, cancellationToken),
                    total,
                    query.Page,
                    query.PageSize
                )
            );
        }

        public async Task<IReadOnlyList<AuditEntryResponse>> ForEntityAsync(
            string entityType,
            object entityId,
            CancellationToken cancellationToken
        )
        {
            var id = Convert.ToString(entityId, System.Globalization.CultureInfo.InvariantCulture);

            var entries = await db
                .AuditEntries.AsNoTracking()
                .Where(e => e.EntityType == entityType && e.EntityId == id)
                .OrderBy(e => e.OccurredAt)
                .ThenBy(e => e.Id)
                .Take(200)
                .ToListAsync(cancellationToken);

            return await ToResponsesAsync(entries, cancellationToken);
        }

        private async Task<IReadOnlyList<AuditEntryResponse>> ToResponsesAsync(
            IReadOnlyList<AuditEntry> entries,
            CancellationToken cancellationToken
        )
        {
            var actorIds = entries.Select(e => e.ActorUserId).Distinct().ToList();

            var emails = await db
                .Users.Where(u => actorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

            return entries
                .Select(e => new AuditEntryResponse(
                    e.Id,
                    e.OccurredAt,
                    new AuditActorResponse(e.ActorUserId, emails.GetValueOrDefault(e.ActorUserId)),
                    e.Action,
                    e.EntityType,
                    e.EntityId,
                    e.Summary,
                    AuditChanges.Parse(e.Changes)
                ))
                .ToList();
        }
    }
}
