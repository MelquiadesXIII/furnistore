using API.Furnistore.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Furnistore.Data.Configurations
{
    // Datos de prueba para que el catálogo público no se vea vacío en desarrollo/demo.
    public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
    {
        public void Configure(EntityTypeBuilder<ProductCategory> builder)
        {
            builder.HasData(
                new ProductCategory { Id = 1, Name = "Sillas" },
                new ProductCategory { Id = 2, Name = "Mesas" },
                new ProductCategory { Id = 3, Name = "Estanterías" },
                new ProductCategory { Id = 4, Name = "Lámparas" }
            );
        }
    }
}
