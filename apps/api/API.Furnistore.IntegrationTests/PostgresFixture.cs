using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Application.Admin.Categories;
using API.Furnistore.Application.Admin.Customers;
using API.Furnistore.Application.Admin.Orders;
using API.Furnistore.Application.Admin.Products;
using API.Furnistore.Application.Carts;
using API.Furnistore.Application.Clients;
using API.Furnistore.Application.Orders;
using API.Furnistore.Application.Products;
using API.Furnistore.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace API.Furnistore.IntegrationTests
{
    public sealed class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:16-alpine").Build();
        private ServiceProvider services = null!;

        public string ConnectionString => container.GetConnectionString();

        public AsyncServiceScope CreateScope() => services.CreateAsyncScope();

        public async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> action)
            where TService : notnull
        {
            await using var scope = CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<TService>());
        }

        public APIFurnistoreContext CreateContext() =>
            services.GetRequiredService<IDbContextFactory<APIFurnistoreContext>>().CreateDbContext();

        public async Task InitializeAsync()
        {
            await container.StartAsync();

            services = BuildServices(container.GetConnectionString());

            await using var db = CreateContext();
            await db.Database.MigrateAsync();
        }

        internal static ServiceProvider BuildServices(string connectionString)
        {
            var collection = new ServiceCollection();
            collection.AddLogging();
            collection.AddDataProtection();
            collection.AddDbContextFactory<APIFurnistoreContext>(options =>
                options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
            );
            collection
                .AddIdentityCore<IdentityUser>(options =>
                {
                    options.Stores.MaxLengthForKeys = 128;
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<APIFurnistoreContext>()
                .AddDefaultTokenProviders();
            collection.AddSingleton(TimeProvider.System);
            collection.AddScoped<ProductService>();
            collection.AddScoped<CartService>();
            collection.AddScoped<ClientService>();
            collection.AddScoped<OrderService>();
            collection.AddScoped<OrderWorkflow>();
            collection.AddScoped<AuditLog>();
            collection.AddScoped<AuditService>();
            collection.AddScoped<AdminOrderService>();
            collection.AddScoped<AdminProductService>();
            collection.AddScoped<AdminCategoryService>();
            collection.AddScoped<AdminCustomerService>();
            return collection.BuildServiceProvider();
        }

        public async Task DisposeAsync()
        {
            await services.DisposeAsync();
            await container.DisposeAsync();
        }
    }
}
