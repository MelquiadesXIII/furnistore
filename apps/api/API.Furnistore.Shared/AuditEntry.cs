namespace API.Furnistore.Shared
{
    public class AuditEntry
    {
        public long Id { get; set; }

        public DateTime OccurredAt { get; set; }

        public string ActorUserId { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string EntityType { get; set; } = string.Empty;

        public string EntityId { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string? Changes { get; set; }
    }
}
