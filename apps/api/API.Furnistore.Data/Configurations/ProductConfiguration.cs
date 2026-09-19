using API.Furnistore.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Furnistore.Data.Configurations
{
    public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.Property(p => p.Price).HasPrecision(12, 2);
            builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");

            builder
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.ProductCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Products_Price_Positive", "\"Price\" > 0");
                table.HasCheckConstraint("CK_Products_Stock_NonNegative", "\"Stock\" >= 0");
                table.HasCheckConstraint(
                    "CK_Products_Dimensions_Positive",
                    "(\"WidthCm\" IS NULL OR \"WidthCm\" > 0) AND (\"DepthCm\" IS NULL OR \"DepthCm\" > 0) AND (\"HeightCm\" IS NULL OR \"HeightCm\" > 0)"
                );
            });
        }
    }
}
