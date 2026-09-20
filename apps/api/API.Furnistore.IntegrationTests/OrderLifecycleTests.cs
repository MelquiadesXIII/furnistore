using API.Furnistore.Application.Orders;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.Furnistore.IntegrationTests
{
    public sealed class OrderLifecycleTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
    {
        private readonly TestData data = new(fixture);

        [Fact]
        public async Task The_owner_can_cancel_a_paid_order_and_the_stock_comes_back()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Cómoda de prueba", 150m, stock: 4);
            var order = await data.PlaceOrderAsync(userId, clientId, product, 3, 150m);
            Assert.Equal(1, await data.StockOfAsync(product));

            var result = await CancelAsync(order.Id, userId, isAdmin: false, reason: "  Me equivoqué de color  ");

            Assert.True(result.IsSuccess, result.Error?.Message);
            Assert.Equal(OrderStatus.Cancelled, result.Value.Status);
            Assert.False(result.Value.CanCancel);
            Assert.NotNull(result.Value.CancelledAt);
            Assert.Equal("Me equivoqué de color", result.Value.CancelReason);
            Assert.Equal(4, await data.StockOfAsync(product));
        }

        [Fact]
        public async Task Cancelling_twice_restores_the_stock_only_once()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Banco de prueba", 40m, stock: 5);
            var order = await data.PlaceOrderAsync(userId, clientId, product, 2, 40m);

            var results = await Task.WhenAll(
                CancelAsync(order.Id, userId, isAdmin: false),
                CancelAsync(order.Id, userId, isAdmin: false)
            );

            Assert.Single(results, result => result.IsSuccess);
            Assert.Single(results, result => !result.IsSuccess && result.Error!.Code == "order.invalid_transition");
            Assert.Equal(5, await data.StockOfAsync(product));
        }

        [Fact]
        public async Task Someone_else_cannot_see_or_cancel_the_order()
        {
            var (ownerId, ownerClient) = await data.CreateClientAsync();
            var (strangerId, _) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Perchero de prueba", 25m, stock: 3);
            var order = await data.PlaceOrderAsync(ownerId, ownerClient, product, 1, 25m);

            var result = await CancelAsync(order.Id, strangerId, isAdmin: false);

            Assert.Equal("order.not_found", result.Error!.Code);
            Assert.Equal(2, await data.StockOfAsync(product));
        }

        [Fact]
        public async Task An_order_moves_from_paid_to_shipped_to_delivered()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Cama de prueba", 900m, stock: 2);
            var order = await data.PlaceOrderAsync(userId, clientId, product, 1, 900m);

            var shipped = await RunAsync(service => service.ShipAsync(order.Id, "admin", CancellationToken.None));
            Assert.True(shipped.IsSuccess, shipped.Error?.Message);
            Assert.Equal(OrderStatus.Shipped, shipped.Value.Status);
            Assert.NotNull(shipped.Value.ShippedAt);
            Assert.False(shipped.Value.CanCancel);

            var delivered = await RunAsync(service => service.DeliverAsync(order.Id, "admin", CancellationToken.None));
            Assert.True(delivered.IsSuccess, delivered.Error?.Message);
            Assert.Equal(OrderStatus.Delivered, delivered.Value.Status);
            Assert.NotNull(delivered.Value.DeliveredAt);
        }

        [Fact]
        public async Task Invalid_transitions_are_rejected_with_a_clear_message()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Escritorio de prueba", 200m, stock: 2);
            var order = await data.PlaceOrderAsync(userId, clientId, product, 1, 200m);

            var deliverPaid = await RunAsync(service => service.DeliverAsync(order.Id, "admin", CancellationToken.None));
            Assert.Equal("order.invalid_transition", deliverPaid.Error!.Code);

            await RunAsync(service => service.ShipAsync(order.Id, "admin", CancellationToken.None));

            var cancelShipped = await CancelAsync(order.Id, userId, isAdmin: false);
            Assert.Equal("order.invalid_transition", cancelShipped.Error!.Code);
            Assert.Equal("No se puede cancelar un pedido enviado.", cancelShipped.Error.Message);
            Assert.Equal(1, await data.StockOfAsync(product));

            var missing = await RunAsync(service => service.ShipAsync(int.MaxValue, "admin", CancellationToken.None));
            Assert.Equal("order.not_found", missing.Error!.Code);
        }

        private Task<Result<OrderResponse>> CancelAsync(int orderId, string userId, bool isAdmin, string? reason = null) =>
            RunAsync(service =>
                service.CancelAsync(orderId, new CancelOrderRequest { Reason = reason }, userId, isAdmin, CancellationToken.None)
            );

        private async Task<Result<OrderResponse>> RunAsync(Func<OrderService, Task<Result<OrderResponse>>> action)
        {
            await using var db = fixture.CreateContext();
            return await action(new OrderService(db, NullLogger<OrderService>.Instance));
        }
    }
}
