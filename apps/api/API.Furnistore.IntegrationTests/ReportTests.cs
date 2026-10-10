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
        public async Task Sales_are_grouped_by_havana_day_and_compared_with_the_previous_period()
        {
            var (_, clientId) = await data.CreateClientAsync();
            var product = await data.CreateProductAsync("Sofá de reporte", 100m, stock: 50);

            await SeedOrderAsync(clientId, Utc(2020, 9, 1, 3, 30), (product, 2, 100m));
            await SeedOrderAsync(clientId, Utc(2020, 9, 1, 5, 0), (product, 1, 100m));
            await SeedOrderAsync(clientId, Utc(2020, 9, 3, 12, 0), (product, 3, 100m));
            await SeedOrderAsync(clientId, Utc(2020, 9, 2, 12, 0), OrderStatus.Cancelled, (product, 4, 100m));

            var report = await RunAsync(service => service.SalesAsync(Range(2020, 9, 1, 2020, 9, 3), CancellationToken.None));

            Assert.Equal(ReportGrouping.Day, report.Meta.GroupBy);
            Assert.Equal(new ReportRange(new(2020, 8, 29), new(2020, 8, 31)), report.Meta.PreviousPeriod);
            Assert.Equal(400m, report.Summary.Revenue.Value);
            Assert.Equal(200m, report.Summary.Revenue.Previous);
            Assert.Equal(1m, report.Summary.Revenue.Change);
            Assert.Equal(2m, report.Summary.Orders.Value);
            Assert.Equal(200m, report.Summary.AverageOrderValue.Value);
            Assert.Equal(4m, report.Summary.Units.Value);

            Assert.Equal([100m, 0m, 300m], report.Series.Select(point => point.Revenue));
            Assert.Equal([0m, 0m, 200m], report.Series.Select(point => point.PreviousRevenue ?? -1));
            Assert.Equal([1, 0, 3], report.Series.Select(point => point.Units));
        }

        [Fact]
        public async Task Products_report_ranks_sales_shares_categories_and_dead_stock()
        {
            var (_, clientId) = await data.CreateClientAsync();
            var categoryId = await CreateCategoryAsync("Exteriores 2019");
            var hammock = await data.CreateProductAsync("Hamaca 2019", 50m, stock: 9);
            var bench = await data.CreateProductAsync("Banco 2019", 150m, stock: 4);
            var forgotten = await data.CreateProductAsync("Tumbona olvidada 2019", 80m, stock: 7);
            await MoveToCategoryAsync(categoryId, hammock, bench, forgotten);
            await BackdateProductAsync(forgotten, Utc(2019, 1, 1, 0, 0));

            await SeedOrderAsync(clientId, Utc(2019, 6, 10, 15, 0), (hammock, 2, 50m), (bench, 1, 150m));
            await SeedOrderAsync(clientId, Utc(2019, 6, 20, 15, 0), (bench, 1, 150m));
            await SeedOrderAsync(clientId, Utc(2018, 6, 20, 15, 0), (forgotten, 1, 80m));

            var report = await RunAsync(service =>
                service.ProductsAsync(Range(2019, 6, 1, 2019, 6, 30), CancellationToken.None)
            );

            Assert.Equal(400m, report.Revenue);
            Assert.Equal(["Banco 2019", "Hamaca 2019"], report.Products.Select(row => row.Name));
            Assert.Equal(new ProductSalesRow(bench, "Banco 2019", "Exteriores 2019", 2, 2, 300m, 0.75m, 150m), report.Products[0]);

            var category = report.Categories.Single(row => row.CategoryId == categoryId);
            Assert.Equal(400m, category.Revenue);
            Assert.Equal(2, category.Orders);
            Assert.Equal(1m, category.Share);

            Assert.Equal(ReportGrouping.Day, report.Meta.GroupBy);
            Assert.Equal(30, report.CategorySeries.Count);
            Assert.Equal(250m, report.CategorySeries[9].Categories.Single(c => c.CategoryId == categoryId).Revenue);

            var unsold = Assert.Single(report.Unsold, row => row.ProductId == forgotten);
            Assert.Equal(Utc(2018, 6, 20, 15, 0), unsold.LastSoldAt);
            Assert.DoesNotContain(report.Unsold, row => row.ProductId == bench);
        }

        [Fact]
        public async Task Operations_report_measures_stages_punctuality_and_overdue_orders()
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
                    order.ProcessingAt = placed.AddHours(2);
                    order.ShippedAt = placed.AddHours(26);
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
                    order.ProcessingAt = placed.AddHours(4);
                    order.ShippedAt = placed.AddHours(52);
                    order.DeliveredAt = Utc(2018, 3, 10, 16, 0);
                    order.EstimatedDeliveryDate = new DateOnly(2018, 3, 8);
                },
                (product, 1, 10m)
            );
            var stuck = await SeedOrderAsync(
                clientId,
                placed,
                OrderStatus.Processing,
                order =>
                {
                    order.ProcessingAt = placed.AddHours(6);
                    order.EstimatedDeliveryDate = new DateOnly(2018, 3, 9);
                },
                (product, 1, 10m)
            );

            var report = await RunAsync(service =>
                service.OperationsAsync(Range(2018, 3, 1, 2018, 3, 31), CancellationToken.None)
            );

            Assert.Equal(3, report.Orders);
            Assert.Equal(2, report.Statuses.Single(row => row.Status == OrderStatus.Delivered).Orders);
            Assert.Equal(0.3333m, report.Statuses.Single(row => row.Status == OrderStatus.Processing).Share);

            var queue = report.Stages.Single(row => row.Stage == FulfillmentStage.Queue);
            Assert.Equal(3, queue.Orders);
            Assert.Equal(4m, queue.AverageHours);
            Assert.Equal(4m, queue.MedianHours);
            Assert.Equal(36m, report.Stages.Single(row => row.Stage == FulfillmentStage.Preparation).AverageHours);

            Assert.Equal(new OnTimeSummary(2, 1, 1, 0.5m, 2m), report.OnTime);

            var overdue = Assert.Single(report.Overdue, row => row.OrderId == stuck);
            Assert.Equal(OrderStatus.Processing, overdue.Status);
            Assert.Equal(StoreCalendar.Today(TimeProvider.System).DayNumber - new DateOnly(2018, 3, 9).DayNumber, overdue.DaysLate);
        }

        [Fact]
        public async Task Cancellations_report_knows_who_cancelled_and_at_which_stage()
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
                service.PrepareAsync(byAdmin.Id, "admin-1", CancellationToken.None)
            );
            await fixture.RunAsync<AdminOrderService, Result<AdminOrderResponse>>(service =>
                service.CancelAsync(byAdmin.Id, new AdminCancelOrderRequest { Reason = "  cambié de idea " }, "admin-1", CancellationToken.None)
            );

            var today = StoreCalendar.Today(TimeProvider.System);
            var report = await RunAsync(service =>
                service.CancellationsAsync(new ReportQuery { From = today, To = today }, CancellationToken.None)
            );

            Assert.Equal(2m, report.Cancelled.Value);
            Assert.Equal(0.6667m, report.Rate.Value);
            Assert.Equal(150m, report.LostRevenue.Value);
            Assert.Null(report.Rate.Change);

            Assert.Equal(
                [new(CancellationActor.Customer, 1, 50m, 0.5m), new CancellationActorRow(CancellationActor.Admin, 1, 100m, 0.5m)],
                report.ByActor
            );
            Assert.Equal(
                [new(OrderStatus.Paid, 1, 0.5m), new CancellationStageRow(OrderStatus.Processing, 1, 0.5m)],
                report.ByStage
            );
            Assert.Equal(new CancellationReasonRow("Cambié de idea", 2, 1m), Assert.Single(report.Reasons));
            Assert.DoesNotContain(report.Orders, row => row.OrderId == kept.Id);
            Assert.Equal(byAdmin.Id, report.Orders[0].OrderId);
        }

        [Fact]
        public async Task Inventory_report_flags_products_that_will_run_out()
        {
            var (_, clientId) = await data.CreateClientAsync();
            var fast = await data.CreateProductAsync("Lámpara veloz 2015", 20m, stock: 10);
            var empty = await data.CreateProductAsync("Repisa agotada 2015", 30m, stock: 0);
            var calm = await data.CreateProductAsync("Baúl tranquilo 2015", 40m, stock: 8);

            await SeedOrderAsync(clientId, Utc(2015, 4, 3, 15, 0), (fast, 20, 20m));

            var report = await RunAsync(service =>
                service.InventoryAsync(Range(2015, 4, 1, 2015, 4, 10), CancellationToken.None)
            );

            var fastRow = report.Products.Single(row => row.ProductId == fast);
            Assert.Equal(2m, fastRow.DailyUnits);
            Assert.Equal(5m, fastRow.DaysOfCover);
            Assert.Equal(StockLevel.Low, fastRow.Level);
            Assert.Equal(200m, fastRow.Value);

            Assert.Equal(StockLevel.OutOfStock, report.Products.Single(row => row.ProductId == empty).Level);

            var calmRow = report.Products.Single(row => row.ProductId == calm);
            Assert.Null(calmRow.DaysOfCover);
            Assert.Equal(StockLevel.Healthy, calmRow.Level);

            Assert.Equal(StockLevel.OutOfStock, report.Products[0].Level);
            Assert.Equal(report.Products.Sum(row => row.Value), report.Summary.Value);
        }

        [Fact]
        public async Task Customers_report_splits_new_and_returning_buyers_and_ranks_zones()
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
                order =>
                {
                    order.ShipToCity = "Santiago";
                    order.ShipToProvince = "Santiago de Cuba";
                },
                (product, 1, 100m)
            );

            var report = await RunAsync(service =>
                service.CustomersAsync(Range(2017, 1, 1, 2017, 1, 31), CancellationToken.None)
            );

            Assert.Equal(2m, report.Buyers.Value);
            Assert.Equal(1m, report.NewBuyers.Value);
            Assert.Equal(1m, report.ReturningBuyers.Value);
            Assert.Equal(200m, report.RevenuePerBuyer.Value);

            Assert.Equal(1, report.Series.Single(point => point.Start == new DateOnly(2017, 1, 5)).ReturningBuyers);
            Assert.Equal(1, report.Series.Single(point => point.Start == new DateOnly(2017, 1, 20)).NewBuyers);

            Assert.Equal([loyal, newcomer], report.TopCustomers.Select(row => row.CustomerId));
            Assert.Equal(0.75m, report.TopCustomers[0].Share);

            Assert.Equal(["La Habana", "Santiago de Cuba"], report.Provinces.Select(row => row.Name));
            Assert.Equal(new RegionRow("Santiago", "Santiago de Cuba", 1, 1, 100m, 0.25m), report.Cities[1]);
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
            Assert.Equal([ReportGrouping.Week, ReportGrouping.Month], yearly.Meta.AvailableGroupings);
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
            Assert.Equal(AuditEntities.Report, entry.EntityType);
            Assert.Equal("Sales", entry.EntityId);
            Assert.Equal("Exportó «Ventas» en PDF (01/09/2026 – 30/09/2026)", entry.Summary);
            var changes = AuditChanges.Parse(entry.Changes);
            Assert.Equal("2026-09-01", changes["from"].To);
            Assert.Equal("2026-09-30", changes["to"].To);
            Assert.Equal("Pdf", changes["format"].To);
        }

        [Fact]
        public void Store_calendar_survives_the_midnight_dst_jump_and_buckets_follow_the_calendar()
        {
            Assert.Equal(Utc(2021, 3, 14, 5, 0), StoreCalendar.StartOfDayUtc(new DateOnly(2021, 3, 14)));
            Assert.Equal(Utc(2021, 3, 15, 4, 0), StoreCalendar.StartOfDayUtc(new DateOnly(2021, 3, 15)));
            Assert.Equal(new DateOnly(2021, 3, 13), StoreCalendar.DateOf(Utc(2021, 3, 14, 4, 59)));

            var weekly = ReportPeriod.Resolve(Range(2026, 9, 3, 2026, 9, 30) with { GroupBy = ReportGrouping.Week }, default).Value;
            Assert.Equal(
                [
                    new(new(2026, 9, 3), new(2026, 9, 6)),
                    new(new(2026, 9, 7), new(2026, 9, 13)),
                    new(new(2026, 9, 14), new(2026, 9, 20)),
                    new(new(2026, 9, 21), new(2026, 9, 27)),
                    new ReportBucket(new(2026, 9, 28), new(2026, 9, 30)),
                ],
                weekly.Buckets()
            );

            var monthly = ReportPeriod.Resolve(Range(2026, 1, 15, 2026, 3, 31), default).Value;
            Assert.Equal(ReportGrouping.Week, monthly.GroupBy);
            Assert.Equal(
                [new(new(2026, 1, 15), new(2026, 1, 31)), new(new(2026, 2, 1), new(2026, 2, 28)), new ReportBucket(new(2026, 3, 1), new(2026, 3, 31))],
                (monthly with { GroupBy = ReportGrouping.Month }).Buckets()
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

        private async Task<int> CreateCategoryAsync(string name)
        {
            await using var db = fixture.CreateContext();
            var category = new ProductCategory { Name = name };
            db.ProductCategories.Add(category);
            await db.SaveChangesAsync();
            return category.Id;
        }

        private async Task MoveToCategoryAsync(int categoryId, params int[] productIds)
        {
            await using var db = fixture.CreateContext();
            await db
                .Products.Where(p => productIds.Contains(p.Id))
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.ProductCategoryId, categoryId));
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
