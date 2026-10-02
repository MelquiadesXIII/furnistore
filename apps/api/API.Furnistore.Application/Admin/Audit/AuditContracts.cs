using System.ComponentModel.DataAnnotations;

namespace API.Furnistore.Application.Admin.Audit
{
    public sealed record AuditQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 50;

        [StringLength(40)]
        public string? EntityType { get; init; }

        [StringLength(64)]
        public string? EntityId { get; init; }

        [StringLength(60)]
        public string? Action { get; init; }

        [StringLength(450)]
        public string? ActorUserId { get; init; }

        public DateTimeOffset? From { get; init; }

        public DateTimeOffset? To { get; init; }
    }

    public sealed record AuditActorResponse(string UserId, string? Email);

    public sealed record AuditEntryResponse(
        long Id,
        DateTime OccurredAt,
        AuditActorResponse Actor,
        string Action,
        string EntityType,
        string EntityId,
        string Summary,
        IReadOnlyDictionary<string, AuditFieldChange> Changes
    );
}
