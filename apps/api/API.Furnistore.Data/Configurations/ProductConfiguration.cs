using API.Furnistore.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Furnistore.Data.Configurations
{
    // Datos de prueba para que el catálogo público no se vea vacío en desarrollo/demo.
    public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasData(
                new Product { Id = 1, Name = "Silla Roble Nórdico", Price = 189.99m, ProductCategoryId = 1 },
                new Product { Id = 2, Name = "Silla Tapizada Lino", Price = 149.50m, ProductCategoryId = 1 },
                new Product { Id = 3, Name = "Silla Alta de Bar Teca", Price = 129.00m, ProductCategoryId = 1 },
                new Product { Id = 4, Name = "Silla Plegable Bambú", Price = 79.90m, ProductCategoryId = 1 },
                new Product { Id = 5, Name = "Mesa de Centro Nogal", Price = 249.00m, ProductCategoryId = 2 },
                new Product { Id = 6, Name = "Mesa Comedor Encino 6 Puestos", Price = 899.00m, ProductCategoryId = 2 },
                new Product { Id = 7, Name = "Mesa Auxiliar Mármol", Price = 199.99m, ProductCategoryId = 2 },
                new Product { Id = 8, Name = "Mesa Escritorio Minimalista", Price = 329.00m, ProductCategoryId = 2 },
                new Product { Id = 9, Name = "Estantería Modular Roble", Price = 259.00m, ProductCategoryId = 3 },
                new Product { Id = 10, Name = "Librero Escalera Pino", Price = 179.00m, ProductCategoryId = 3 },
                new Product { Id = 12, Name = "Vitrina Vintage", Price = 449.00m, ProductCategoryId = 3 },
                new Product { Id = 13, Name = "Lámpara de Pie Arco", Price = 159.00m, ProductCategoryId = 4 },
                new Product { Id = 14, Name = "Lámpara de Mesa Cerámica", Price = 69.90m, ProductCategoryId = 4 },
                new Product { Id = 15, Name = "Lámpara Colgante Rattan", Price = 99.00m, ProductCategoryId = 4 },
                new Product { Id = 16, Name = "Lámpara LED Escritorio", Price = 45.50m, ProductCategoryId = 4 }
            );
        }
    }
}
