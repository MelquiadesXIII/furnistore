using API.Furnistore.Application.Admin.Orders;
using API.Furnistore.Application.Orders;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;

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

            var result = await CancelAsync(order.Id, userId, reason: "  Me equivoqué de color  ");

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

            var results = await Task.WhenAll(CancelAsync(order.Id, userId), CancelAsync(order.Id, userId));

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

            var cancel = await CancelAsync(order.Id, strangerId);
            var read = await fixture.RunAsync<OrderService, Result<OrderResponse>>(service =>
                service.GetByIdAsync(order.Id, strangerId, CancellationToken.None)
            );

            Assert.Equal("order.not_found", cancel.Error!.Code);
            Assert.Equal("order.not_found", read.Error!.Code);
            Assert.Equal(2, await data.StockOfAsync(product));
        }

        [Fact]
        public async Task An_order_moves_from_paid_to_processing_to_shipped_to_delivered()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Cama de prueba", 900m, stock: 2);
            var order = await data.PlaceOrderAsync(userId, clientId, product, 1, 900m);

            var prepared = await AdminAsync(service => service.PrepareAsync(order.Id, "admin-1", CancellationToken.None));
            Assert.True(prepared.IsSuccess, prepared.Error?.Message);
            Assert.Equal(OrderStatus.Processing, prepared.Value.Order.Status);
            Assert.NotNull(prepared.Value.Order.ProcessingAt);
            Assert.False(prepared.Value.Order.CanCancel);
            Assert.Equal([AdminOrderAction.Ship, AdminOrderAction.Cancel], prepared.Value.AvailableActions);

            var shipped = await AdminAsync(service => service.ShipAsync(order.Id, "admin-1", CancellationToken.None));
            Assert.Equal(OrderStatus.Shipped, shipped.Value.Order.Status);
            Assert.NotNull(shipped.Value.Order.ShippedAt);
            Assert.Equal([AdminOrderAction.Deliver], shipped.Value.AvailableActions);

            var delivered = await AdminAsync(service => service.DeliverAsync(order.Id, "admin-1", CancellationToken.None));
            Assert.Equal(OrderStatus.Delivered, delivered.Value.Order.Status);
            Assert.NotNull(delivered.Value.Order.DeliveredAt);
            Assert.Empty(delivered.Value.AvailableActions);

            Assert.Equal(
                ["order.processing", "order.shipped", "order.delivered"],
                delivered.Value.History.Select(entry => entry.Action)
            );
            Assert.All(delivered.Value.History, entry => Assert.Equal("admin-1", entry.Actor.UserId));
            Assert.Equal($"Pedido #{order.OrderNumber}: Pagado → En preparación", delivered.Value.History[0].Summary);
            Assert.Equal(new("Paid", "Processing"), delivered.Value.History[0].Changes["status"]);
        }

        [Fact]
        public async Task Once_in_preparation_only_the_admin_can_cancel_and_the_stock_comes_back()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Aparador de prueba", 300m, stock: 3);
            var order = await data.PlaceOrderAsync(userId, clientId, product, 2, 300m);
            await AdminAsync(service => service.PrepareAsync(order.Id, "admin-1", CancellationToken.None));

            var byCustomer = await CancelAsync(order.Id, userId);
            Assert.Equal("order.invalid_transition", byCustomer.Error!.Code);
            Assert.Equal("No se puede cancelar un pedido en preparación.", byCustomer.Error.Message);
            Assert.Equal(1, await data.StockOfAsync(product));

            var byAdmin = await AdminAsync(service =>
                service.CancelAsync(order.Id, new AdminCancelOrderRequest { Reason = "Sin material" }, "admin-1", CancellationToken.None)
            );
            Assert.True(byAdmin.IsSuccess, byAdmin.Error?.Message);
            Assert.Equal(OrderStatus.Cancelled, byAdmin.Value.Order.Status);
            Assert.Equal("Sin material", byAdmin.Value.Order.CancelReason);
            Assert.Equal(3, await data.StockOfAsync(product));
            Assert.Equal("Sin material", byAdmin.Value.History[^1].Changes["cancelReason"].To);
        }

        [Fact]
        public async Task Invalid_transitions_are_rejected_with_a_clear_message()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Escritorio de prueba", 200m, stock: 2);
            var order = await data.PlaceOrderAsync(userId, clientId, product, 1, 200m);

            var shipPaid = await AdminAsync(service => service.ShipAsync(order.Id, "admin-1", CancellationToken.None));
            Assert.Equal("order.invalid_transition", shipPaid.Error!.Code);
            Assert.Equal("No se puede enviar un pedido pagado.", shipPaid.Error.Message);

            var deliverPaid = await AdminAsync(service => service.DeliverAsync(order.Id, "admin-1", CancellationToken.None));
            Assert.Equal("order.invalid_transition", deliverPaid.Error!.Code);

            await AdminAsync(service => service.PrepareAsync(order.Id, "admin-1", CancellationToken.None));
            await AdminAsync(service => service.ShipAsync(order.Id, "admin-1", CancellationToken.None));

            var cancelShipped = await AdminAsync(service =>
                service.CancelAsync(order.Id, new AdminCancelOrderRequest(), "admin-1", CancellationToken.None)
            );
            Assert.Equal("No se puede cancelar un pedido enviado.", cancelShipped.Error!.Message);
            Assert.Equal(1, await data.StockOfAsync(product));

            var missing = await AdminAsync(service => service.ShipAsync(int.MaxValue, "admin-1", CancellationToken.None));
            Assert.Equal("order.not_found", missing.Error!.Code);
        }

        [Fact]
        public async Task Two_admins_preparing_at_once_produce_a_single_transition()
        {
            var (userId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Vitrina de prueba", 120m, stock: 2);
            var order = await data.PlaceOrderAsync(userId, clientId, product, 1, 120m);

            var results = await Task.WhenAll(
                AdminAsync(service => service.PrepareAsync(order.Id, "admin-1", CancellationToken.None)),
                AdminAsync(service => service.PrepareAsync(order.Id, "admin-2", CancellationToken.None))
            );

            Assert.Single(results, result => result.IsSuccess);
            var detail = await AdminAsync(service => service.GetByIdAsync(order.Id, CancellationToken.None));
            Assert.Single(detail.Value.History);
        }

        private Task<Result<OrderResponse>> CancelAsync(int orderId, string userId, string? reason = null) =>
            fixture.RunAsync<OrderService, Result<OrderResponse>>(service =>
                service.CancelAsync(orderId, new CancelOrderRequest { Reason = reason }, userId, CancellationToken.None)
            );

        private Task<Result<AdminOrderResponse>> AdminAsync(Func<AdminOrderService, Task<Result<AdminOrderResponse>>> action) =>
            fixture.RunAsync(action);
    }
}
