using API.Furnistore.Shared;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace API.Furnistore.Data
{
    public class APIFurnistoreContext : IdentityDbContext
    {
        public APIFurnistoreContext(DbContextOptions options) : base(options) { }

        public DbSet<Client> Clients { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<ProductCategory> ProductCategories { get; set; }

        public DbSet<OrderDetail> OrderDetails { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(APIFurnistoreContext).Assembly);

            modelBuilder.Entity<Client>()
                .HasOne<IdentityUser>()
                .WithOne()
                .HasForeignKey<Client>(client => client.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Client>()
                .HasIndex(client => client.UserId)
                .IsUnique();

            modelBuilder.Entity<CartItem>()
                .HasIndex(item => new { item.ClientId, item.ProductId })
                .IsUnique();

            // El numero de orden lo asigna la base de datos (secuencia), nunca el cliente.
            modelBuilder.HasSequence<int>("order_number_seq").StartsAt(1000);

            modelBuilder.Entity<Order>()
                .Property(order => order.OrderNumber)
                .HasDefaultValueSql("nextval('order_number_seq')");

            modelBuilder.Entity<Order>()
                .HasIndex(order => order.OrderNumber)
                .IsUnique();
        }
    }
}
