using API.Furnistore.Shared;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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

        public DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasSequence<int>("order_number_seq").StartsAt(1000);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(APIFurnistoreContext).Assembly);
        }
    }
}
