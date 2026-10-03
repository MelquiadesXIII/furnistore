using API.Furnistore.Application.Orders;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Furnistore.IntegrationTests
{
    internal sealed class TestData(PostgresFixture fixture)
    {
        public async Task<(string UserId, int ClientId)> CreateClientAsync(bool profileComplete = true)
        {
            await using var db = fixture.CreateContext();
            var email = $"{Guid.NewGuid():N}@example.com";
            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            db.Users.Add(user);

            var client = new Client
            {
                UserId = user.Id,
                FirstName = "Ana",
                LastName = "Prueba",
                Phone = profileComplete ? "+15551234567" : null,
                Street = profileComplete ? "Calle Real 123" : null,
                City = profileComplete ? "Centro" : null,
                Province = profileComplete ? "La Habana" : null,
                DeliveryNotes = profileComplete ? "Portón azul" : null,
            };
            db.Clients.Add(client);
            await db.SaveChangesAsync();

            return (user.Id, client.ID);
        }

        public async Task<int> CreateProductAsync(string name, decimal price, int stock, bool isActive = true)
        {
            await using var db = fixture.CreateContext();
            var product = new Product
            {
                Name = name,
                Price = price,
                Stock = stock,
                IsActive = isActive,
                ProductCategoryId = await db.ProductCategories.Select(c => c.Id).FirstAsync(),
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product.Id;
        }

        public async Task AddToCartAsync(int clientId, int productId, int quantity)
        {
            await using var db = fixture.CreateContext();
            db.CartItems.Add(new CartItem { ClientId = clientId, ProductId = productId, Quantity = quantity });
            await db.SaveChangesAsync();
        }

        public Task<Result<OrderResponse>> CheckoutAsync(string userId, decimal expectedTotal) =>
            fixture.RunAsync<OrderService, Result<OrderResponse>>(service =>
                service.CheckoutAsync(new CheckoutRequest { ExpectedTotal = expectedTotal }, userId, CancellationToken.None)
            );

        public async Task<OrderResponse> PlaceOrderAsync(string userId, int clientId, int productId, int quantity, decimal unitPrice)
        {
            await AddToCartAsync(clientId, productId, quantity);
            var result = await CheckoutAsync(userId, unitPrice * quantity);
            Assert.True(result.IsSuccess, result.Error?.Message);
            return result.Value;
        }

        public async Task<int> StockOfAsync(int productId)
        {
            await using var db = fixture.CreateContext();
            return await db.Products.Where(p => p.Id == productId).Select(p => p.Stock).SingleAsync();
        }
    }
}
