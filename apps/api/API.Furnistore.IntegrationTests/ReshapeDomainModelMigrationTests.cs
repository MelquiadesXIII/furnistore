using API.Furnistore.Application.Auth;
using API.Furnistore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace API.Furnistore.IntegrationTests
{
    public sealed class ReshapeDomainModelMigrationTests : IAsyncLifetime
    {
        private const string PreviousMigration = "20261002013109_AddCheckoutSupport";

        private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:16-alpine").Build();
        private ServiceProvider services = null!;

        public async Task InitializeAsync()
        {
            await container.StartAsync();
            services = PostgresFixture.BuildServices(container.GetConnectionString());
        }

        public async Task DisposeAsync()
        {
            await services.DisposeAsync();
            await container.DisposeAsync();
        }

        [Fact]
        public async Task Legacy_rows_are_converted_and_the_migration_can_be_reverted()
        {
            await using var db = services.GetRequiredService<IDbContextFactory<APIFurnistoreContext>>().CreateDbContext();
            var migrator = db.GetService<IMigrator>();

            await migrator.MigrateAsync(PreviousMigration);
            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "AspNetUsers" ("Id", "Email", "UserName", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
                VALUES ('u-pending', 'pending@example.com', 'pending@example.com', true, false, false, false, 0),
                       ('u-real', 'real@example.com', 'real@example.com', true, false, false, false, 0);

                INSERT INTO "Clients" ("ID", "UserId", "FirstName", "LastName", "BirthDate", "Phone", "Address")
                VALUES (501, 'u-pending', 'Pedro', 'Pendiente', '2000-01-01', '+10000000000', 'Pendiente de completar'),
                       (502, 'u-real', 'Rosa', 'Real', '1990-05-05', '+5355555555', '  Calle 23 #456, Vedado  ');

                INSERT INTO "ProductCategories" ("Id", "Name") VALUES (901, 'Categoría heredada');
                INSERT INTO "Products" ("Id", "Name", "Price", "Stock", "ProductCategoryId")
                VALUES (801, 'Mesa heredada', 100.00, 5, 901);

                INSERT INTO "Orders" ("Id", "ClientId", "OrderDate", "DeliveryDate", "Status", "Total")
                VALUES (701, 502, '2026-09-01T10:00:00Z', '2026-09-08T00:00:00Z', 3, 200.00),
                       (702, 502, '2026-09-02T10:00:00Z', '2026-09-09T00:00:00Z', 0, 100.00);

                INSERT INTO "OrderDetails" ("OrderId", "ProductId", "ProductName", "Quantity", "UnitPrice")
                VALUES (701, 801, 'Mesa heredada', 2, 100.00),
                       (702, 801, 'Mesa heredada', 1, 100.00);

                INSERT INTO "RefreshTokens" ("UserId", "Token", "JwtId", "IsUsed", "IsRevoked", "AddedDate", "ExpiryDate")
                VALUES ('u-real', 'legacy-refresh-token', 'jti-1', false, false, now(), now() + interval '30 days'),
                       ('u-gone', 'orphan-refresh-token', 'jti-2', false, false, now(), now() + interval '30 days');
                """
            );

            await migrator.MigrateAsync();

            var pending = await db.Clients.AsNoTracking().SingleAsync(c => c.ID == 501);
            Assert.Null(pending.Phone);
            Assert.Null(pending.Street);
            Assert.False(pending.IsProfileComplete);

            var real = await db.Clients.AsNoTracking().SingleAsync(c => c.ID == 502);
            Assert.Equal("+5355555555", real.Phone);
            Assert.Equal("Calle 23 #456, Vedado", real.Street);
            Assert.Null(real.City);

            var delivered = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == 701);
            Assert.Equal(Shared.OrderStatus.Delivered, delivered.Status);
            Assert.Equal(200m, delivered.Subtotal);
            Assert.Equal(0m, delivered.ShippingCost);
            Assert.Equal(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), delivered.PaidAt);
            Assert.Equal(new DateOnly(2026, 9, 8), delivered.EstimatedDeliveryDate);
            Assert.Equal(new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc), delivered.DeliveredAt);
            Assert.NotNull(delivered.ShippedAt);
            Assert.Equal("Rosa Real", delivered.ShipToName);
            Assert.Equal("+5355555555", delivered.ShipToPhone);
            Assert.Equal("Calle 23 #456, Vedado", delivered.ShipToStreet);

            var formerlyPending = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == 702);
            Assert.Equal(Shared.OrderStatus.Paid, formerlyPending.Status);
            Assert.Equal(
                ["(none)"],
                await db.Database
                    .SqlQueryRaw<string>(
                        """SELECT coalesce(column_default, '(none)') AS "Value" FROM information_schema.columns WHERE table_name = 'Orders' AND column_name = 'Status'"""
                    )
                    .ToListAsync()
            );

            var product = await db.Products.AsNoTracking().SingleAsync(p => p.Id == 801);
            Assert.True(product.IsActive);
            Assert.Equal(string.Empty, product.Description);

            var tokens = await db.RefreshTokens.AsNoTracking().ToListAsync();
            var token = Assert.Single(tokens);
            Assert.Equal(RefreshTokenHasher.Hash("legacy-refresh-token"), token.TokenHash);

            await migrator.MigrateAsync(PreviousMigration);

            Assert.Equal(
                ["3", "1"],
                await db.Database
                    .SqlQueryRaw<string>("""SELECT "Status"::text AS "Value" FROM "Orders" ORDER BY "Id" """)
                    .ToListAsync()
            );
            Assert.Equal(
                ["Pendiente de completar", "Calle 23 #456, Vedado"],
                await db.Database
                    .SqlQueryRaw<string>("""SELECT "Address" AS "Value" FROM "Clients" ORDER BY "ID" """)
                    .ToListAsync()
            );
            Assert.Equal(
                ["+10000000000", "+5355555555"],
                await db.Database
                    .SqlQueryRaw<string>("""SELECT "Phone" AS "Value" FROM "Clients" ORDER BY "ID" """)
                    .ToListAsync()
            );

            await migrator.MigrateAsync();
            Assert.Equal(2, await db.Orders.CountAsync());
        }
    }
}
