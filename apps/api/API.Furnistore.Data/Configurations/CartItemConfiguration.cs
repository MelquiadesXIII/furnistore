using API.Furnistore.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Furnistore.Data.Configurations
{
    public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.HasIndex(item => new { item.ClientId, item.ProductId }).IsUnique();

            builder
                .HasOne<Client>()
                .WithMany()
                .HasForeignKey(item => item.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne<Product>()
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable(table =>
                table.HasCheckConstraint("CK_CartItems_Quantity_Positive", "\"Quantity\" > 0")
            );
        }
    }
}
