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

        public AsyncServiceScope CreateScope() => services.CreateAsyncScope();

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
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<APIFurnistoreContext>()
                .AddDefaultTokenProviders();
            return collection.BuildServiceProvider();
        }

        public async Task DisposeAsync()
        {
            await services.DisposeAsync();
            await container.DisposeAsync();
        }
    }
}
