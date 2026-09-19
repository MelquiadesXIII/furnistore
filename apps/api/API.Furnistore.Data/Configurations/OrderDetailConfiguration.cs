using API.Furnistore.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Furnistore.Data.Configurations
{
    public sealed class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
    {
        public void Configure(EntityTypeBuilder<OrderDetail> builder)
        {
            builder.HasKey(od => new { od.OrderId, od.ProductId });

            builder.Property(od => od.UnitPrice).HasPrecision(12, 2);

            builder
                .HasOne<Product>()
                .WithMany()
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_OrderDetails_Quantity_Positive", "\"Quantity\" > 0");
                table.HasCheckConstraint("CK_OrderDetails_UnitPrice_NonNegative", "\"UnitPrice\" >= 0");
            });
        }
    }
}
