using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Application.Admin.Orders;
using API.Furnistore.Application.Admin.Reports;
using API.Furnistore.Application.Orders;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace API.Furnistore.IntegrationTests
{
    public sealed class ReportTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
    {
        private readonly TestData data = new(fixture);

        [Fact]
        public async Task Sales_are_grouped_by_havana_day()
        {
            var (_, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Sofá de reporte", 100m, stock: 50);

            await SeedOrderAsync(clientId, Utc(2020, 9, 1, 3, 30), (product, 2, 100m));
            await SeedOrderAsync(clientId, Utc(2020, 9, 1, 5, 0), (product, 1, 100m));
            await SeedOrderAsync(clientId, Utc(2020, 9, 3, 12, 0), (product, 3, 100m));
            await SeedOrderAsync(clientId, Utc(2020, 9, 2, 12, 0), OrderStatus.Cancelled, (product, 4, 100m));

            var report = await RunAsync(service => service.SalesAsync(Range(2020, 9, 1, 2020, 9, 3), CancellationToken.None));

            Assert.Equal(ReportGrouping.Day, report.Meta.GroupBy);
            Assert.Equal(400m, report.Revenue);
            Assert.Equal(2, report.Orders);
            Assert.Equal(200m, report.AverageOrderValue);
            Assert.Equal(4, report.Units);
            Assert.Equal([100m, 0m, 300m], report.Series.Select(point => point.Revenue));
            Assert.Equal([1, 0, 3], report.Series.Select(point => point.Units));
        }

        [Fact]
        public async Task Products_report_ranks_sales_and_counts_products_without_sales()
        {
            var (_, clientId) = await data.CreateClientAsync();
            var hammock = await data.CreateProductAsync("Hamaca 2019", 50m, stock: 9);
            var bench = await data.CreateProductAsync("Banco 2019", 150m, stock: 4);
            var forgotten = await data.CreateProductAsync("Tumbona olvidada 2019", 80m, stock: 7);
            await BackdateProductAsync(forgotten, Utc(2019, 1, 1, 0, 0));

            await SeedOrderAsync(clientId, Utc(2019, 6, 10, 15, 0), (hammock, 2, 50m), (bench, 1, 150m));
            await SeedOrderAsync(clientId, Utc(2019, 6, 20, 15, 0), (bench, 1, 150m));

            var report = await RunAsync(service =>
                service.ProductsAsync(Range(2019, 6, 1, 2019, 6, 30), CancellationToken.None)
            );

            Assert.Equal(400m, report.Revenue);
            Assert.Equal(["Banco 2019", "Hamaca 2019"], report.Products.Select(row => row.Name));
            Assert.Equal(2, report.Products[0].Units);
            Assert.Equal(300m, report.Products[0].Revenue);
            Assert.Equal(0.75m, report.Products[0].Share);
            Assert.Equal(1, report.UnsoldProducts);
        }

        [Fact]
        public async Task Operations_report_counts_statuses_punctuality_and_overdue_orders()
        {
            var (_, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Mesa 2018", 10m, stock: 20);
            var placed = Utc(2018, 3, 5, 14, 0);

            await SeedOrderAsync(
                clientId,
                placed,
                OrderStatus.Delivered,
                order =>
                {
                    order.DeliveredAt = placed.AddHours(74);
                    order.EstimatedDeliveryDate = new DateOnly(2018, 3, 8);
                },
                (product, 1, 10m)
            );
            await SeedOrderAsync(
                clientId,
                placed,
                OrderStatus.Delivered,
                order =>
                {
                    order.DeliveredAt = Utc(2018, 3, 10, 16, 0);
                    order.EstimatedDeliveryDate = new DateOnly(2018, 3, 8);
                },
                (product, 1, 10m)
            );
            var stuck = await SeedOrderAsync(
                clientId,
                placed,
                OrderStatus.Processing,
                order => order.EstimatedDeliveryDate = new DateOnly(2018, 3, 9),
                (product, 1, 10m)
            );

            var report = await RunAsync(service =>
                service.OperationsAsync(Range(2018, 3, 1, 2018, 3, 31), CancellationToken.None)
            );

            Assert.Equal(3, report.Orders);
            Assert.Equal(2, report.Statuses.Single(row => row.Status == OrderStatus.Delivered).Orders);
            Assert.Equal(1, report.Statuses.Single(row => row.Status == OrderStatus.Processing).Orders);
            Assert.Equal(2, report.Delivered);
            Assert.Equal(1, report.DeliveredOnTime);
            Assert.Equal(0.5m, report.OnTimeRate);

            var overdue = Assert.Single(report.Overdue, row => row.OrderId == stuck);
            Assert.Equal(
                StoreCalendar.Today(TimeProvider.System).DayNumber - new DateOnly(2018, 3, 9).DayNumber,
                overdue.DaysLate
            );
        }

        [Fact]
        public async Task Cancellations_report_knows_who_cancelled()
        {
            var (customerUserId, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Silla de hoy", 25m, stock: 30);

            var kept = await data.PlaceOrderAsync(customerUserId, clientId, product, 1, 25m);
            var byCustomer = await data.PlaceOrderAsync(customerUserId, clientId, product, 2, 25m);
            var byAdmin = await data.PlaceOrderAsync(customerUserId, clientId, product, 4, 25m);

            await fixture.RunAsync<OrderService, Result<OrderResponse>>(service =>
                service.CancelAsync(byCustomer.Id, new CancelOrderRequest { Reason = "Cambié de idea" }, customerUserId, CancellationToken.None)
            );
            await fixture.RunAsync<AdminOrderService, Result<AdminOrderResponse>>(service =>
                service.CancelAsync(byAdmin.Id, new AdminCancelOrderRequest { Reason = "Sin material" }, "admin-1", CancellationToken.None)
            );

            var today = StoreCalendar.Today(TimeProvider.System);
            var report = await RunAsync(service =>
                service.CancellationsAsync(new ReportQuery { From = today, To = today }, CancellationToken.None)
            );

            Assert.Equal(2, report.Cancelled);
            Assert.Equal(0.6667m, report.Rate);
            Assert.Equal(150m, report.LostRevenue);
            Assert.Equal(
                [new(CancellationActor.Customer, 1, 0.5m), new CancellationActorRow(CancellationActor.Admin, 1, 0.5m)],
                report.ByActor
            );
            Assert.DoesNotContain(report.Orders, row => row.OrderId == kept.Id);
            Assert.Equal("Sin material", report.Orders.Single(row => row.OrderId == byAdmin.Id).Reason);
        }

        [Fact]
        public async Task Inventory_report_flags_low_and_empty_stock()
        {
            var low = await data.CreateProductAsync("Lámpara escasa 2015", 20m, stock: 3);
            var empty = await data.CreateProductAsync("Repisa agotada 2015", 30m, stock: 0);
            var plenty = await data.CreateProductAsync("Baúl tranquilo 2015", 40m, stock: 8);

            var report = await RunAsync(service => service.InventoryAsync(new ReportQuery(), CancellationToken.None));

            Assert.Equal(StockLevel.Low, report.Products.Single(row => row.ProductId == low).Level);
            Assert.Equal(StockLevel.OutOfStock, report.Products.Single(row => row.ProductId == empty).Level);
            Assert.Equal(StockLevel.Healthy, report.Products.Single(row => row.ProductId == plenty).Level);
            Assert.Equal(320m, report.Products.Single(row => row.ProductId == plenty).Value);
            Assert.Equal(report.Products.Sum(row => row.Value), report.Value);
            Assert.Equal(report.ActiveProducts, report.OutOfStock + report.LowStock + report.Healthy);
        }

        [Fact]
        public async Task Customers_report_splits_new_and_returning_buyers()
        {
            var (_, loyal) = await data.CreateClientAsync();
            var (_, newcomer) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Cómoda 2017", 100m, stock: 50);

            await SeedOrderAsync(loyal, Utc(2016, 12, 10, 15, 0), (product, 1, 100m));
            await SeedOrderAsync(loyal, Utc(2017, 1, 5, 15, 0), (product, 3, 100m));
            await SeedOrderAsync(
                newcomer,
                Utc(2017, 1, 20, 15, 0),
                OrderStatus.Paid,
                order => order.ShipToProvince = "Santiago de Cuba",
                (product, 1, 100m)
            );

            var report = await RunAsync(service =>
                service.CustomersAsync(Range(2017, 1, 1, 2017, 1, 31), CancellationToken.None)
            );

            Assert.Equal(2, report.Buyers);
            Assert.Equal(1, report.NewBuyers);
            Assert.Equal(1, report.ReturningBuyers);
            Assert.Equal([loyal, newcomer], report.TopCustomers.Select(row => row.CustomerId));
            Assert.Equal(new ProvinceRow("La Habana", 1, 300m), report.Provinces[0]);
            Assert.Equal(new ProvinceRow("Santiago de Cuba", 1, 100m), report.Provinces[1]);
        }

        [Fact]
        public async Task Invalid_ranges_are_rejected_and_unavailable_groupings_fall_back()
        {
            var backwards = await fixture.RunAsync<AdminReportService, Result<SalesReport>>(service =>
                service.SalesAsync(Range(2020, 2, 1, 2020, 1, 1), CancellationToken.None)
            );
            Assert.Equal("report.invalid_range", backwards.Error!.Code);

            var tooLong = await fixture.RunAsync<AdminReportService, Result<SalesReport>>(service =>
                service.SalesAsync(Range(2010, 1, 1, 2020, 1, 1), CancellationToken.None)
            );
            Assert.Equal("report.range_too_long", tooLong.Error!.Code);

            var yearly = await RunAsync(service =>
                service.SalesAsync(Range(2014, 1, 1, 2014, 12, 31) with { GroupBy = ReportGrouping.Day }, CancellationToken.None)
            );
            Assert.Equal(ReportGrouping.Month, yearly.Meta.GroupBy);
            Assert.Equal(12, yearly.Series.Count);
        }

        [Fact]
        public async Task Exports_are_recorded_in_the_audit_log()
        {
            var result = await fixture.RunAsync<AdminReportService, Result>(service =>
                service.RecordExportAsync(
                    new ReportExportRequest
                    {
                        Report = ReportKind.Sales,
                        Format = ReportFormat.Pdf,
                        From = new DateOnly(2026, 9, 1),
                        To = new DateOnly(2026, 9, 30),
                    },
                    "admin-7",
                    CancellationToken.None
                )
            );
            Assert.True(result.IsSuccess);

            await using var db = fixture.CreateContext();
            var entry = await db.AuditEntries.SingleAsync(e => e.ActorUserId == "admin-7");
            Assert.Equal(AuditActions.ReportExported, entry.Action);
            Assert.Equal("Sales", entry.EntityId);
            Assert.Equal("Exportó «Ventas» en PDF (01/09/2026 – 30/09/2026)", entry.Summary);
            Assert.Equal("2026-09-01", AuditChanges.Parse(entry.Changes)["from"].To);
        }

        [Fact]
        public void Store_calendar_survives_the_midnight_dst_jump_and_weeks_start_on_monday()
        {
            Assert.Equal(Utc(2021, 3, 14, 5, 0), StoreCalendar.StartOfDayUtc(new DateOnly(2021, 3, 14)));
            Assert.Equal(new DateOnly(2021, 3, 13), StoreCalendar.DateOf(Utc(2021, 3, 14, 4, 59)));

            var weekly = ReportPeriod.Resolve(Range(2026, 9, 3, 2026, 9, 20) with { GroupBy = ReportGrouping.Week }, default).Value;
            Assert.Equal(
                [
                    new(new(2026, 9, 3), new(2026, 9, 6)),
                    new(new(2026, 9, 7), new(2026, 9, 13)),
                    new ReportBucket(new(2026, 9, 14), new(2026, 9, 20)),
                ],
                weekly.Buckets()
            );
        }

        private async Task<T> RunAsync<T>(Func<AdminReportService, Task<Result<T>>> action)
        {
            var result = await fixture.RunAsync(action);
            Assert.True(result.IsSuccess, result.Error?.Message);
            return result.Value;
        }

        private static ReportQuery Range(int fromYear, int fromMonth, int fromDay, int toYear, int toMonth, int toDay) =>
            new() { From = new DateOnly(fromYear, fromMonth, fromDay), To = new DateOnly(toYear, toMonth, toDay) };

        private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
            new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

        private Task<int> SeedOrderAsync(int clientId, DateTime placedAt, params (int ProductId, int Quantity, decimal UnitPrice)[] lines) =>
            SeedOrderAsync(clientId, placedAt, OrderStatus.Paid, _ => { }, lines);

        private Task<int> SeedOrderAsync(
            int clientId,
            DateTime placedAt,
            OrderStatus status,
            params (int ProductId, int Quantity, decimal UnitPrice)[] lines
        ) => SeedOrderAsync(clientId, placedAt, status, _ => { }, lines);

        private async Task<int> SeedOrderAsync(
            int clientId,
            DateTime placedAt,
            OrderStatus status,
            Action<Order> customize,
            params (int ProductId, int Quantity, decimal UnitPrice)[] lines
        )
        {
            await using var db = fixture.CreateContext();
            var subtotal = lines.Sum(line => line.Quantity * line.UnitPrice);
            var order = new Order
            {
                ClientId = clientId,
                Status = status,
                Subtotal = subtotal,
                Total = subtotal,
                ShipToName = "Ana Prueba",
                ShipToPhone = "+15551234567",
                ShipToStreet = "Calle Real 123",
                ShipToCity = "Centro",
                ShipToProvince = "La Habana",
                PlacedAt = placedAt,
                PaidAt = placedAt,
                EstimatedDeliveryDate = DateOnly.FromDateTime(placedAt).AddDays(5),
                CancelledAt = status == OrderStatus.Cancelled ? placedAt.AddHours(1) : null,
                OrderDetails = lines
                    .Select(line => new OrderDetail
                    {
                        ProductId = line.ProductId,
                        ProductName = "Producto",
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                    })
                    .ToList(),
            };
            customize(order);
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            return order.Id;
        }

        private async Task BackdateProductAsync(int productId, DateTime createdAt)
        {
            await using var db = fixture.CreateContext();
            await db
                .Products.Where(p => p.Id == productId)
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.CreatedAt, createdAt));
        }
    }
}
