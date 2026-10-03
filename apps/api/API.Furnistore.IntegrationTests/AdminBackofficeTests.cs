using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Application.Admin.Categories;
using API.Furnistore.Application.Admin.Customers;
using API.Furnistore.Application.Admin.Orders;
using API.Furnistore.Application.Admin.Products;
using API.Furnistore.Application.Clients;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace API.Furnistore.IntegrationTests
{
    public sealed class AdminBackofficeTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
    {
        private readonly TestData data = new(fixture);

        [Fact]
        public async Task Creating_and_editing_a_product_is_audited_field_by_field()
        {
            var categoryId = await FirstCategoryIdAsync();

            var created = await Products(service => service.CreateAsync(NewProduct("Sofá Lino", categoryId), "admin-1", CancellationToken.None));
            Assert.True(created.IsSuccess, created.Error?.Message);
            Assert.False(created.Value.HasOrders);

            var updated = await Products(service =>
                service.UpdateAsync(
                    created.Value.Id,
                    Edit(created.Value, price: 899.5m, isActive: false),
                    "admin-1",
                    CancellationToken.None
                )
            );
            Assert.True(updated.IsSuccess, updated.Error?.Message);
            Assert.False(updated.Value.IsActive);
            Assert.NotEqual(created.Value.Version, updated.Value.Version);

            var history = await History(AuditEntities.Product, created.Value.Id);
            Assert.Equal(["product.created", "product.updated"], history.Select(entry => entry.Action));
            Assert.Equal("Producto «Sofá Lino» archivado", history[1].Summary);
            Assert.Equal(new("750", "899.5"), history[1].Changes["price"]);
            Assert.Equal(new("true", "false"), history[1].Changes["isActive"]);
            Assert.Equal(["isActive", "price"], history[1].Changes.Keys);
        }

        [Fact]
        public async Task A_stale_product_version_is_rejected_instead_of_overwriting()
        {
            var categoryId = await FirstCategoryIdAsync();
            var original = (await Products(service => service.CreateAsync(NewProduct("Mesa Pino", categoryId), "admin-1", CancellationToken.None))).Value;

            var first = await Products(service => service.UpdateAsync(original.Id, Edit(original, stock: 3), "admin-1", CancellationToken.None));
            Assert.True(first.IsSuccess, first.Error?.Message);

            var second = await Products(service => service.UpdateAsync(original.Id, Edit(original, stock: 9), "admin-2", CancellationToken.None));
            Assert.Equal("admin.version_conflict", second.Error!.Code);
            Assert.Equal(3, await data.StockOfAsync(original.Id));
        }

        [Fact]
        public async Task A_product_that_was_ordered_cannot_be_deleted_only_archived()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Taburete vendido", 20m, stock: 5);
            await data.PlaceOrderAsync(userId, clientId, product, 1, 20m);

            var detail = await Products(service => service.GetByIdAsync(product, CancellationToken.None));
            Assert.True(detail.Value.HasOrders);

            var deleted = await fixture.RunAsync<AdminProductService, Result>(service =>
                service.DeleteAsync(product, "admin-1", CancellationToken.None)
            );
            Assert.Equal("product.referenced_by_order", deleted.Error!.Code);
        }

        [Fact]
        public async Task Product_search_filters_by_status_and_low_stock_and_validates_the_sort_key()
        {
            var marker = Guid.NewGuid().ToString("N")[..8];
            await data.CreateProductAsync($"A {marker}", 10m, stock: 1);
            await data.CreateProductAsync($"B {marker}", 30m, stock: 50);
            await data.CreateProductAsync($"C {marker}", 20m, stock: 2, isActive: false);

            var low = await Search(new AdminProductQuery { Search = marker, MaxStock = 2, Sort = "-price" });
            Assert.Equal([$"C {marker}", $"A {marker}"], low.Value.Items.Select(p => p.Name));

            var active = await Search(new AdminProductQuery { Search = marker, Status = ProductListStatus.Active, Sort = "-stock" });
            Assert.Equal([$"B {marker}", $"A {marker}"], active.Value.Items.Select(p => p.Name));

            var invalid = await Search(new AdminProductQuery { Search = marker, Sort = "password" });
            Assert.Equal("query.invalid_sort", invalid.Error!.Code);
        }

        [Fact]
        public async Task Categories_report_their_products_and_names_stay_unique()
        {
            var name = $"Exterior {Guid.NewGuid():N}"[..20];
            var created = await Categories(service => service.CreateAsync(new SaveCategoryRequest { Name = name }, "admin-1", CancellationToken.None));
            Assert.True(created.IsSuccess, created.Error?.Message);

            var duplicate = await Categories(service =>
                service.CreateAsync(new SaveCategoryRequest { Name = name.ToUpperInvariant() }, "admin-1", CancellationToken.None)
            );
            Assert.Equal("category.name_taken", duplicate.Error!.Code);

            var racing = await Task.WhenAll(
                Enumerable.Range(0, 4).Select(_ =>
                    Categories(service =>
                        service.CreateAsync(new SaveCategoryRequest { Name = name + " 2" }, "admin-1", CancellationToken.None)
                    )
                )
            );
            Assert.Single(racing, result => result.IsSuccess);
            Assert.All(racing.Where(result => !result.IsSuccess), result => Assert.Equal("category.name_taken", result.Error!.Code));

            var listed = await fixture.RunAsync<AdminCategoryService, Result<PagedResult<AdminCategoryResponse>>>(service =>
                service.SearchAsync(new AdminCategoryQuery { Search = name, Sort = "-productCount" }, CancellationToken.None)
            );
            Assert.Equal(2, listed.Value.Total);
            Assert.All(listed.Value.Items, category => Assert.Equal(0, category.ProductCount));
        }

        [Fact]
        public async Task Order_search_finds_by_number_or_customer_and_stats_count_each_status()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Silla de búsqueda", 15m, stock: 10);
            var first = await data.PlaceOrderAsync(userId, clientId, product, 2, 15m);
            var second = await data.PlaceOrderAsync(userId, clientId, product, 1, 15m);
            var before = (await fixture.RunAsync<AdminOrderService, Result<AdminOrderStats>>(service => service.StatsAsync(CancellationToken.None))).Value;

            await fixture.RunAsync<AdminOrderService, Result<AdminOrderResponse>>(service =>
                service.PrepareAsync(second.Id, "admin-1", CancellationToken.None)
            );

            var byNumber = await Orders(new AdminOrderQuery { Search = $"#{first.OrderNumber}" });
            var single = Assert.Single(byNumber.Value.Items);
            Assert.Equal(first.Id, single.Id);
            Assert.Equal(2, single.ItemCount);
            Assert.Equal("Ana Prueba", single.Customer.Name);
            Assert.Equal(clientId, single.Customer.Id);

            var byCustomer = await Orders(new AdminOrderQuery { CustomerId = clientId, Sort = "placedAt" });
            Assert.Equal([first.Id, second.Id], byCustomer.Value.Items.Select(o => o.Id));

            var processing = await Orders(new AdminOrderQuery { CustomerId = clientId, Status = OrderStatus.Processing });
            Assert.Equal([second.Id], processing.Value.Items.Select(o => o.Id));

            var after = (await fixture.RunAsync<AdminOrderService, Result<AdminOrderStats>>(service => service.StatsAsync(CancellationToken.None))).Value;
            Assert.Equal(before.Processing + 1, after.Processing);
            Assert.Equal(before.Paid - 1, after.Paid);
        }

        [Fact]
        public async Task Editing_a_customer_checks_the_version_and_the_detail_shows_cart_orders_and_history()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Lámpara de cliente", 40m, stock: 10);
            await data.PlaceOrderAsync(userId, clientId, product, 2, 40m);
            await data.AddToCartAsync(clientId, product, 3);

            var detail = (await Customers(service => service.GetByIdAsync(clientId, CancellationToken.None))).Value;
            Assert.Equal(80m, detail.TotalSpent);
            Assert.Equal(1, detail.OrderCount);
            Assert.Equal(120m, Assert.Single(detail.Cart).LineTotal);
            Assert.Single(detail.RecentOrders);

            var request = new UpdateCustomerRequest
            {
                FirstName = "Ana María",
                LastName = "Prueba",
                Phone = "+5355555555",
                Address = new ShippingAddress { Street = "Calle 10 #5", City = "Vedado", Province = "La Habana" },
                Version = detail.Version,
            };

            var updated = await Customers(service => service.UpdateAsync(clientId, request, "admin-1", CancellationToken.None));
            Assert.True(updated.IsSuccess, updated.Error?.Message);
            Assert.Equal("Ana María", updated.Value.FirstName);
            Assert.Equal(new("Ana", "Ana María"), Assert.Single(updated.Value.History).Changes["firstName"]);

            var stale = await Customers(service => service.UpdateAsync(clientId, request with { FirstName = "Otra" }, "admin-2", CancellationToken.None));
            Assert.Equal("admin.version_conflict", stale.Error!.Code);
        }

        [Fact]
        public async Task A_temporarily_locked_account_can_be_unlocked()
        {
            var (userId, clientId) = await data.CreateClientAsync();

            await using (var db = fixture.CreateContext())
            {
                var user = await db.Users.SingleAsync(u => u.Id == userId);
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5);
                user.AccessFailedCount = 5;
                await db.SaveChangesAsync();
            }

            var locked = await Customers(service => service.GetByIdAsync(clientId, CancellationToken.None));
            Assert.NotNull(locked.Value.Account.LockedOutUntil);

            var listed = await fixture.RunAsync<AdminCustomerService, Result<PagedResult<AdminCustomerSummary>>>(service =>
                service.SearchAsync(new AdminCustomerQuery { Filter = CustomerFilter.LockedOut, PageSize = 100 }, CancellationToken.None)
            );
            Assert.Contains(listed.Value.Items, customer => customer.Id == clientId && customer.IsLockedOut);

            var unlocked = await Customers(service => service.UnlockAsync(clientId, "admin-1", CancellationToken.None));
            Assert.True(unlocked.IsSuccess, unlocked.Error?.Message);
            Assert.Null(unlocked.Value.Account.LockedOutUntil);
            Assert.Equal(0, unlocked.Value.Account.FailedLoginCount);

            var again = await Customers(service => service.UnlockAsync(clientId, "admin-1", CancellationToken.None));
            Assert.Equal("customer.not_locked", again.Error!.Code);
        }

        private static CreateProductRequest NewProduct(string name, int categoryId) =>
            new()
            {
                Name = name,
                Price = 750m,
                Stock = 4,
                ProductCategoryId = categoryId,
                Material = "Roble",
            };

        private static UpdateProductRequest Edit(
            AdminProductResponse product,
            decimal? price = null,
            int? stock = null,
            bool? isActive = null
        ) =>
            new()
            {
                Name = product.Name,
                Description = product.Description,
                Price = price ?? product.Price,
                Stock = stock ?? product.Stock,
                ProductCategoryId = product.Category.Id,
                ImageUrl = product.ImageUrl,
                WidthCm = product.WidthCm,
                DepthCm = product.DepthCm,
                HeightCm = product.HeightCm,
                Material = product.Material,
                IsActive = isActive ?? product.IsActive,
                Version = product.Version,
            };

        private async Task<int> FirstCategoryIdAsync()
        {
            await using var db = fixture.CreateContext();
            return await db.ProductCategories.Select(c => c.Id).FirstAsync();
        }

        private Task<IReadOnlyList<AuditEntryResponse>> History(string entityType, int id) =>
            fixture.RunAsync<AuditService, IReadOnlyList<AuditEntryResponse>>(service =>
                service.ForEntityAsync(entityType, id, CancellationToken.None)
            );

        private Task<Result<AdminProductResponse>> Products(Func<AdminProductService, Task<Result<AdminProductResponse>>> action) =>
            fixture.RunAsync(action);

        private Task<Result<PagedResult<AdminProductResponse>>> Search(AdminProductQuery query) =>
            fixture.RunAsync<AdminProductService, Result<PagedResult<AdminProductResponse>>>(service =>
                service.SearchAsync(query, CancellationToken.None)
            );

        private Task<Result<AdminCategoryResponse>> Categories(Func<AdminCategoryService, Task<Result<AdminCategoryResponse>>> action) =>
            fixture.RunAsync(action);

        private Task<Result<PagedResult<AdminOrderSummary>>> Orders(AdminOrderQuery query) =>
            fixture.RunAsync<AdminOrderService, Result<PagedResult<AdminOrderSummary>>>(service =>
                service.SearchAsync(query, CancellationToken.None)
            );

        private Task<Result<AdminCustomerResponse>> Customers(Func<AdminCustomerService, Task<Result<AdminCustomerResponse>>> action) =>
            fixture.RunAsync(action);
    }
}
