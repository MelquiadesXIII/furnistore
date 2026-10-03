using API.Furnistore.Application.Admin.Products;
using API.Furnistore.Application.Carts;
using API.Furnistore.Application.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.Furnistore.IntegrationTests
{
    public sealed class CatalogAndCartTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
    {
        private readonly TestData data = new(fixture);

        [Fact]
        public async Task Archived_products_are_hidden_from_the_public_catalog_but_listed_in_the_backoffice()
        {
            var marker = Guid.NewGuid().ToString("N")[..8];
            await data.CreateProductAsync($"Visible {marker}", 10m, stock: 1);
            await data.CreateProductAsync($"Archivado {marker}", 10m, stock: 1, isActive: false);

            var visitor = await fixture.RunAsync<ProductService, IReadOnlyList<ProductResponse>>(async service =>
                (await service.SearchAsync(new ProductQuery { Search = marker }, CancellationToken.None)).Value.Items
            );
            var all = await AdminSearchAsync(new AdminProductQuery { Search = marker });
            var archived = await AdminSearchAsync(new AdminProductQuery { Search = marker, Status = ProductListStatus.Archived });

            Assert.Equal([$"Visible {marker}"], visitor.Select(p => p.Name));
            Assert.Equal(2, all.Count);
            Assert.Equal([$"Archivado {marker}"], archived.Select(p => p.Name));
        }

        [Fact]
        public async Task A_product_embeds_its_category_and_stays_readable_when_archived()
        {
            var product = await data.CreateProductAsync("Silla archivada", 30m, stock: 0, isActive: false);

            await using var db = fixture.CreateContext();
            var category = await db.Products.Where(p => p.Id == product).Select(p => p.Category.Name).SingleAsync();
            var result = await new ProductService(db, NullLogger<ProductService>.Instance)
                .GetByIdAsync(product, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.False(result.Value.IsActive);
            Assert.Equal(category, result.Value.Category.Name);
            Assert.Equal(string.Empty, result.Value.Description);
        }

        [Fact]
        public async Task The_cart_rejects_archived_products()
        {
            var (userId, _) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Lámpara archivada", 15m, stock: 9, isActive: false);

            await using var db = fixture.CreateContext();
            var result = await new CartService(db, NullLogger<CartService>.Instance).AddItemAsync(
                userId,
                new AddCartItemRequest { ProductId = product, Quantity = 1 },
                CancellationToken.None
            );

            Assert.Equal("cart.product_unavailable", result.Error!.Code);
        }

        [Fact]
        public async Task The_cart_computes_line_totals_shipping_and_total_on_the_server()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var chair = await data.CreateProductAsync("Silla de cuenta", 19.99m, stock: 10);
            var table = await data.CreateProductAsync("Mesa de cuenta", 120m, stock: 10);
            await data.AddToCartAsync(clientId, chair, 3);
            await data.AddToCartAsync(clientId, table, 1);

            await using var db = fixture.CreateContext();
            var cart = (await new CartService(db, NullLogger<CartService>.Instance).GetAsync(userId, CancellationToken.None)).Value;

            Assert.Equal([chair, table], cart.Items.Select(i => i.ProductId));
            Assert.Equal(59.97m, cart.Items[0].LineTotal);
            Assert.Equal(179.97m, cart.Subtotal);
            Assert.Equal(0m, cart.ShippingCost);
            Assert.Equal(179.97m, cart.Total);
            Assert.All(cart.Items, item => Assert.True(item.IsActive));
        }

        private Task<IReadOnlyList<AdminProductResponse>> AdminSearchAsync(AdminProductQuery query) =>
            fixture.RunAsync<AdminProductService, IReadOnlyList<AdminProductResponse>>(async service =>
                (await service.SearchAsync(query, CancellationToken.None)).Value.Items
            );
    }
}
