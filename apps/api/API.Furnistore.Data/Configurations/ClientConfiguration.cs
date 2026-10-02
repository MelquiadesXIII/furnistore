using API.Furnistore.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Furnistore.Data.Configurations
{
    public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            builder
                .HasOne<IdentityUser>()
                .WithOne()
                .HasForeignKey<Client>(client => client.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(client => client.UserId).IsUnique();
            builder.Property(client => client.Version).IsRowVersion();
        }
    }
}
