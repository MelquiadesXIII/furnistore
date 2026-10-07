using System.Globalization;
using System.Text.Json;
using API.Furnistore.Data;
using API.Furnistore.Shared;

namespace API.Furnistore.Application.Admin.Audit
{
    public static class AuditActors
    {
        public const string Console = "console";
    }

    public static class AuditEntities
    {
        public const string Order = "Order";
        public const string Product = "Product";
        public const string Category = "ProductCategory";
        public const string Customer = "Customer";
        public const string Report = "Report";
    }

    public static class AuditActions
    {
        public const string OrderProcessing = "order.processing";
        public const string OrderShipped = "order.shipped";
        public const string OrderDelivered = "order.delivered";
        public const string OrderCancelled = "order.cancelled";
        public const string ProductCreated = "product.created";
        public const string ProductUpdated = "product.updated";
        public const string ProductDeleted = "product.deleted";
        public const string CategoryCreated = "category.created";
        public const string CategoryUpdated = "category.updated";
        public const string CategoryDeleted = "category.deleted";
        public const string CustomerUpdated = "customer.updated";
        public const string CustomerUnlocked = "customer.unlocked";
        public const string CustomerDisabled = "customer.disabled";
        public const string CustomerEnabled = "customer.enabled";
        public const string AdminGranted = "customer.admin_granted";
        public const string AdminRevoked = "customer.admin_revoked";
        public const string ReportExported = "report.exported";
    }

    public sealed record AuditFieldChange(string? From, string? To);

    public sealed class AuditChanges
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly SortedDictionary<string, AuditFieldChange> fields = new(StringComparer.Ordinal);

        public AuditChanges Track<T>(string field, T before, T after)
        {
            if (!EqualityComparer<T>.Default.Equals(before, after))
                fields[field] = new AuditFieldChange(Format(before), Format(after));
            return this;
        }

        public AuditChanges Set<T>(string field, T value)
        {
            fields[field] = new AuditFieldChange(null, Format(value));
            return this;
        }

        public bool IsEmpty => fields.Count == 0;

        public string? ToJson() => IsEmpty ? null : JsonSerializer.Serialize(fields, Json);

        public static IReadOnlyDictionary<string, AuditFieldChange> Parse(string? json) =>
            new SortedDictionary<string, AuditFieldChange>(
                string.IsNullOrEmpty(json)
                    ? []
                    : JsonSerializer.Deserialize<Dictionary<string, AuditFieldChange>>(json, Json) ?? [],
                StringComparer.Ordinal
            );

        private static string? Format<T>(T value) =>
            value switch
            {
                null => null,
                bool flag => flag ? "true" : "false",
                decimal number => number.ToString("0.############", CultureInfo.InvariantCulture),
                DateTime moment => moment.ToString("O", CultureInfo.InvariantCulture),
                DateOnly day => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString(),
            };
    }

    public sealed class AuditLog(APIFurnistoreContext db, TimeProvider clock)
    {
        public void Record(
            string actorUserId,
            string action,
            string entityType,
            object entityId,
            string summary,
            AuditChanges? changes = null
        ) =>
            db.AuditEntries.Add(
                new AuditEntry
                {
                    OccurredAt = clock.GetUtcNow().UtcDateTime,
                    ActorUserId = actorUserId,
                    Action = action,
                    EntityType = entityType,
                    EntityId = Convert.ToString(entityId, CultureInfo.InvariantCulture) ?? string.Empty,
                    Summary = summary.Length > 300 ? summary[..300] : summary,
                    Changes = changes?.ToJson(),
                }
            );
    }
}
