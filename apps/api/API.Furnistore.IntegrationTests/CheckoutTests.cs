using API.Furnistore.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.Furnistore.IntegrationTests
{
    public sealed class CheckoutTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
    {
        private readonly TestData data = new(fixture);

        [Fact]
        public async Task Checkout_creates_a_paid_order_snapshots_prices_and_empties_the_cart()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var chair = await data.CreateProductAsync("Silla de prueba", 100m, stock: 5);
            var lamp = await data.CreateProductAsync("Lámpara de prueba", 49.99m, stock: 2);
            await data.AddToCartAsync(clientId, chair, 2);
            await data.AddToCartAsync(clientId, lamp, 1);

            var result = await data.CheckoutAsync(userId, expectedTotal: 249.99m);

            Assert.True(result.IsSuccess, result.Error?.Message);
            var order = result.Value;
            Assert.Equal(OrderStatus.Paid, order.Status);
            Assert.True(order.CanCancel);
            Assert.Equal(249.99m, order.Subtotal);
            Assert.Equal(0m, order.ShippingCost);
            Assert.Equal(249.99m, order.Total);
            Assert.True(order.OrderNumber >= 1000);
            Assert.Equal(order.PlacedAt, order.PaidAt);
            Assert.Equal(DateOnly.FromDateTime(order.PlacedAt).AddDays(7), order.EstimatedDeliveryDate);
            Assert.Null(order.ShippedAt);
            Assert.Contains(order.Lines, line => line.ProductName == "Silla de prueba" && line.UnitPrice == 100m && line.Quantity == 2 && line.LineTotal == 200m);
            Assert.Contains(order.Lines, line => line.ProductName == "Lámpara de prueba" && line.UnitPrice == 49.99m && line.Quantity == 1 && line.LineTotal == 49.99m);

            Assert.Equal(3, await data.StockOfAsync(chair));
            Assert.Equal(1, await data.StockOfAsync(lamp));

            await using var db = fixture.CreateContext();
            Assert.False(await db.CartItems.AnyAsync(item => item.ClientId == clientId));

            await db.Products.Where(p => p.Id == chair).ExecuteUpdateAsync(s => s.SetProperty(p => p.Price, 999m));
            var stored = await db.OrderDetails.SingleAsync(d => d.OrderId == order.Id && d.ProductId == chair);
            Assert.Equal(100m, stored.UnitPrice);
        }

        [Fact]
        public async Task Checkout_freezes_the_shipping_address_on_the_order()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Aparador de prueba", 300m, stock: 3);

            var order = await data.PlaceOrderAsync(userId, clientId, product, 1, 300m);

            Assert.Equal("Ana Prueba", order.ShipTo.Name);
            Assert.Equal("+15551234567", order.ShipTo.Phone);
            Assert.Equal("Calle Real 123", order.ShipTo.Address.Street);
            Assert.Equal("Centro", order.ShipTo.Address.City);
            Assert.Equal("La Habana", order.ShipTo.Address.Province);
            Assert.Equal("Portón azul", order.ShipTo.Address.DeliveryNotes);

            await using var db = fixture.CreateContext();
            await db.Clients
                .Where(c => c.ID == clientId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Street, "Otra Calle 9").SetProperty(c => c.City, "Playa"));

            var stored = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == order.Id);
            Assert.Equal("Calle Real 123", stored.ShipToStreet);
            Assert.Equal("Centro", stored.ShipToCity);
        }

        [Fact]
        public async Task Checkout_with_an_empty_cart_is_rejected()
        {
            var (userId, _) = await data.CreateClientAsync();

            var result = await data.CheckoutAsync(userId, expectedTotal: 0m);

            Assert.False(result.IsSuccess);
            Assert.Equal("checkout.empty_cart", result.Error!.Code);
        }

        [Fact]
        public async Task Checkout_with_an_incomplete_profile_is_rejected_without_touching_stock()
        {
            var (userId, clientId) = await data.CreateClientAsync(profileComplete: false);
            var product = await data.CreateProductAsync("Mesa de prueba", 80m, stock: 4);
            await data.AddToCartAsync(clientId, product, 1);

            var result = await data.CheckoutAsync(userId, expectedTotal: 80m);

            Assert.Equal("checkout.profile_incomplete", result.Error!.Code);
            Assert.Equal(4, await data.StockOfAsync(product));
            await using var db = fixture.CreateContext();
            Assert.True(await db.CartItems.AnyAsync(item => item.ClientId == clientId));
        }

        [Fact]
        public async Task Checkout_with_an_archived_product_is_rejected_and_names_it()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Sofá retirado", 500m, stock: 2);
            await data.AddToCartAsync(clientId, product, 1);

            await using (var db = fixture.CreateContext())
                await db.Products.Where(p => p.Id == product).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));

            var result = await data.CheckoutAsync(userId, expectedTotal: 500m);

            Assert.Equal("checkout.product_unavailable", result.Error!.Code);
            Assert.Contains("Sofá retirado", result.Error.Message);
            Assert.Equal(2, await data.StockOfAsync(product));
        }

        [Fact]
        public async Task Checkout_with_a_stale_total_is_rejected_and_keeps_the_cart()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Librero de prueba", 120m, stock: 3);
            await data.AddToCartAsync(clientId, product, 1);

            var result = await data.CheckoutAsync(userId, expectedTotal: 99m);

            Assert.Equal("checkout.price_changed", result.Error!.Code);
            Assert.Equal(3, await data.StockOfAsync(product));
            await using var db = fixture.CreateContext();
            Assert.True(await db.CartItems.AnyAsync(item => item.ClientId == clientId));
        }

        [Fact]
        public async Task Checkout_with_insufficient_stock_is_rejected_and_names_the_product()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Vitrina de prueba", 300m, stock: 1);
            await data.AddToCartAsync(clientId, product, 2);

            var result = await data.CheckoutAsync(userId, expectedTotal: 600m);

            Assert.Equal("checkout.insufficient_stock", result.Error!.Code);
            Assert.Contains("Vitrina de prueba", result.Error.Message);
            Assert.Equal(1, await data.StockOfAsync(product));
        }

        [Fact]
        public async Task Concurrent_checkouts_of_the_same_cart_create_exactly_one_order()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Estantería de prueba", 50m, stock: 10);
            await data.AddToCartAsync(clientId, product, 2);

            var results = await Task.WhenAll(
                data.CheckoutAsync(userId, expectedTotal: 100m),
                data.CheckoutAsync(userId, expectedTotal: 100m)
            );

            Assert.Single(results, result => result.IsSuccess);
            Assert.Single(results, result => !result.IsSuccess && result.Error!.Code == "checkout.empty_cart");
            Assert.Equal(8, await data.StockOfAsync(product));
            await using var db = fixture.CreateContext();
            Assert.Equal(1, await db.Orders.CountAsync(order => order.ClientId == clientId));
        }

        [Fact]
        public async Task Concurrent_clients_never_oversell_the_last_unit()
        {
            var product = await data.CreateProductAsync("Silla única", 75m, stock: 1);
            var (firstUser, firstClient) = await data.CreateClientAsync();
            var (secondUser, secondClient) = await data.CreateClientAsync();
            await data.AddToCartAsync(firstClient, product, 1);
            await data.AddToCartAsync(secondClient, product, 1);

            var results = await Task.WhenAll(
                data.CheckoutAsync(firstUser, expectedTotal: 75m),
                data.CheckoutAsync(secondUser, expectedTotal: 75m)
            );

            Assert.Single(results, result => result.IsSuccess);
            Assert.Single(results, result => !result.IsSuccess && result.Error!.Code == "checkout.insufficient_stock");
            Assert.Equal(0, await data.StockOfAsync(product));
        }

        [Fact]
        public async Task Deleting_a_product_removes_it_from_every_cart()
        {
            var (_, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Producto descontinuado", 10m, stock: 5);
            await data.AddToCartAsync(clientId, product, 1);

            await using var db = fixture.CreateContext();
            await db.Products.Where(p => p.Id == product).ExecuteDeleteAsync();

            Assert.False(await db.CartItems.AnyAsync(item => item.ProductId == product));
        }

        [Fact]
        public async Task The_database_refuses_to_delete_a_product_that_was_sold()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Producto vendido", 20m, stock: 5);
            await data.PlaceOrderAsync(userId, clientId, product, 1, 20m);

            await using var db = fixture.CreateContext();
            var error = await Assert.ThrowsAsync<PostgresException>(
                () => db.Products.Where(p => p.Id == product).ExecuteDeleteAsync()
            );

            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
        }
    }
}
