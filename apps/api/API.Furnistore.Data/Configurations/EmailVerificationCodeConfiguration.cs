using API.Furnistore.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Furnistore.Data.Configurations
{
    public sealed class EmailVerificationCodeConfiguration : IEntityTypeConfiguration<EmailVerificationCode>
    {
        public void Configure(EntityTypeBuilder<EmailVerificationCode> builder)
        {
            builder.Property(code => code.CodeHash).HasMaxLength(64);

            builder.HasIndex(code => code.UserId).IsUnique();

            builder
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(code => code.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable(table =>
                table.HasCheckConstraint("CK_EmailVerificationCodes_FailedAttempts", "\"FailedAttempts\" >= 0")
            );
        }
    }
}
