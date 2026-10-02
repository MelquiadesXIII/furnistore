using API.Furnistore.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Furnistore.Data.Configurations
{
    public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
    {
        public void Configure(EntityTypeBuilder<AuditEntry> builder)
        {
            builder.Property(entry => entry.ActorUserId).HasMaxLength(450);
            builder.Property(entry => entry.Action).HasMaxLength(60);
            builder.Property(entry => entry.EntityType).HasMaxLength(40);
            builder.Property(entry => entry.EntityId).HasMaxLength(64);
            builder.Property(entry => entry.Summary).HasMaxLength(300);
            builder.Property(entry => entry.Changes).HasColumnType("jsonb");

            builder.HasIndex(entry => new { entry.EntityType, entry.EntityId, entry.OccurredAt });
            builder.HasIndex(entry => entry.OccurredAt);
            builder.HasIndex(entry => entry.ActorUserId);
        }
    }
}
