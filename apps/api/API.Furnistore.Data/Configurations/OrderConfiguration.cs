using API.Furnistore.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Furnistore.Data.Configurations
{
    public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.Property(order => order.OrderNumber).HasDefaultValueSql("nextval('order_number_seq')");
            builder.HasIndex(order => order.OrderNumber).IsUnique();

            builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(order => order.Subtotal).HasPrecision(12, 2);
            builder.Property(order => order.ShippingCost).HasPrecision(12, 2);
            builder.Property(order => order.Total).HasPrecision(12, 2);

            builder
                .HasOne<Client>()
                .WithMany()
                .HasForeignKey(order => order.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(order => new { order.ClientId, order.PlacedAt });

            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Orders_Status",
                    "\"Status\" IN ('Paid', 'Shipped', 'Delivered', 'Cancelled')"
                );
                table.HasCheckConstraint(
                    "CK_Orders_Amounts",
                    "\"Subtotal\" >= 0 AND \"ShippingCost\" >= 0 AND \"Total\" = \"Subtotal\" + \"ShippingCost\""
                );
            });
        }
    }
}
