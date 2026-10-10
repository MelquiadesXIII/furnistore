using System.Globalization;
using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace API.Furnistore.Application.Admin.Reports
{
    public sealed class AdminReportService(APIFurnistoreContext db, AuditLog audit, TimeProvider clock)
    {
        public const int LowStockThreshold = 3;
        public const int CoverAlertDays = 14;
        private const int TopCustomersLimit = 10;
        private const int CitiesLimit = 15;
        private const int ReasonsLimit = 10;

        private static readonly OrderStatus[] OpenStatuses = [OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped];

        public async Task<Result<SalesReport>> SalesAsync(ReportQuery query, CancellationToken cancellationToken)
        {
            var resolved = Resolve(query);
            if (!resolved.IsSuccess)
                return Result.Fail<SalesReport>(resolved.Error!);

            var period = resolved.Value;
            var current = Fold(period, await DailySalesAsync(period, cancellationToken));
            var previous = Fold(period.Previous, await DailySalesAsync(period.Previous, cancellationToken));

            var series = current
                .Select((bucket, index) =>
                {
                    var before = index < previous.Count ? previous[index] : null;
                    return new SalesPoint(
                        bucket.Start,
                        bucket.End,
                        bucket.Revenue,
                        bucket.Orders,
                        bucket.Units,
                        ReportMath.Average(bucket.Revenue, bucket.Orders),
                        before?.Revenue,
                        before?.Orders
                    );
                })
                .ToList();

            var now = Totals(current);
            var then = Totals(previous);

            return Result.Ok(
                new SalesReport(
                    Meta(period),
                    new SalesSummary(
                        ReportMath.Metric(now.Revenue, then.Revenue),
                        ReportMath.Metric(now.Orders, then.Orders),
                        ReportMath.Metric(
                            ReportMath.Average(now.Revenue, now.Orders),
                            ReportMath.Average(then.Revenue, then.Orders)
                        ),
                        ReportMath.Metric(now.Units, then.Units)
                    ),
                    series
                )
            );
        }

        public async Task<Result<ProductsReport>> ProductsAsync(ReportQuery query, CancellationToken cancellationToken)
        {
            var resolved = Resolve(query);
            if (!resolved.IsSuccess)
                return Result.Fail<ProductsReport>(resolved.Error!);

            var period = resolved.Value;
            var lines = SoldLines(period);

            var byProduct = await lines
                .GroupBy(line => line.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Units = g.Sum(line => line.Quantity),
                    Revenue = g.Sum(line => line.Amount),
                    Orders = g.Select(line => line.OrderId).Distinct().Count(),
                })
                .ToListAsync(cancellationToken);

            var byCategory = await (
                from line in lines
                join product in db.Products on line.ProductId equals product.Id
                group line by product.ProductCategoryId into g
                select new
                {
                    CategoryId = g.Key,
                    Units = g.Sum(line => line.Quantity),
                    Revenue = g.Sum(line => line.Amount),
                    Orders = g.Select(line => line.OrderId).Distinct().Count(),
                }
            ).ToListAsync(cancellationToken);

            var categoryDays = await (
                from line in lines
                join product in db.Products on line.ProductId equals product.Id
                group line.Amount by new
                {
                    Day = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(line.PlacedAt, StoreCalendar.TimeZoneId).Date,
                    product.ProductCategoryId,
                } into g
                select new
                {
                    g.Key.Day,
                    g.Key.ProductCategoryId,
                    Revenue = g.Sum(),
                }
            ).ToListAsync(cancellationToken);

            var catalog = await Catalog(cancellationToken);
            var categories = await db
                .ProductCategories.AsNoTracking()
                .Select(c => new { c.Id, c.Name })
                .ToListAsync(cancellationToken);

            var revenue = byProduct.Sum(row => row.Revenue);

            var products = byProduct
                .Select(row =>
                {
                    var product = catalog.GetValueOrDefault(row.ProductId);
                    return new ProductSalesRow(
                        row.ProductId,
                        product?.Name ?? $"Producto {row.ProductId}",
                        product?.Category ?? string.Empty,
                        row.Units,
                        row.Orders,
                        row.Revenue,
                        ReportMath.Share(row.Revenue, revenue),
                        ReportMath.Average(row.Revenue, row.Units)
                    );
                })
                .OrderByDescending(row => row.Revenue)
                .ThenByDescending(row => row.Units)
                .ThenBy(row => row.Name, StringComparer.CurrentCulture)
                .ToList();

            var categoryRows = categories
                .Select(category =>
                {
                    var sold = byCategory.FirstOrDefault(row => row.CategoryId == category.Id);
                    return new CategorySalesRow(
                        category.Id,
                        category.Name,
                        sold?.Units ?? 0,
                        sold?.Orders ?? 0,
                        sold?.Revenue ?? 0,
                        ReportMath.Share(sold?.Revenue ?? 0, revenue)
                    );
                })
                .OrderByDescending(row => row.Revenue)
                .ThenBy(row => row.Name, StringComparer.CurrentCulture)
                .ToList();

            var soldCategoryIds = categoryRows.Where(row => row.Revenue > 0).Select(row => row.CategoryId).ToList();

            var categorySeries = period
                .Buckets()
                .Select(bucket => new CategorySeriesPoint(
                    bucket.Start,
                    bucket.End,
                    soldCategoryIds
                        .Select(categoryId => new CategoryAmount(
                            categoryId,
                            categoryDays
                                .Where(day =>
                                    day.ProductCategoryId == categoryId && bucket.Contains(DateOnly.FromDateTime(day.Day))
                                )
                                .Sum(day => day.Revenue)
                        ))
                        .ToList()
                ))
                .ToList();

            var soldIds = byProduct.Select(row => row.ProductId).ToHashSet();
            var endUtc = period.EndUtc;
            var unsold = catalog
                .Values.Where(product => product.IsActive && product.CreatedAt < endUtc && !soldIds.Contains(product.Id))
                .ToList();
            var unsoldIds = unsold.Select(product => product.Id).ToList();

            var lastSold = await (
                from detail in db.OrderDetails
                join order in db.Orders on detail.OrderId equals order.Id
                where order.Status != OrderStatus.Cancelled && unsoldIds.Contains(detail.ProductId)
                group order.PlacedAt by detail.ProductId into g
                select new { ProductId = g.Key, LastSoldAt = g.Max() }
            ).ToDictionaryAsync(row => row.ProductId, row => row.LastSoldAt, cancellationToken);

            var unsoldRows = unsold
                .Select(product => new UnsoldProductRow(
                    product.Id,
                    product.Name,
                    product.Category,
                    product.Price,
                    product.Stock,
                    lastSold.TryGetValue(product.Id, out var at) ? at : null
                ))
                .OrderByDescending(row => row.Stock)
                .ThenBy(row => row.Name, StringComparer.CurrentCulture)
                .ToList();

            return Result.Ok(new ProductsReport(Meta(period), revenue, products, categoryRows, categorySeries, unsoldRows));
        }

        public async Task<Result<OperationsReport>> OperationsAsync(ReportQuery query, CancellationToken cancellationToken)
        {
            var resolved = Resolve(query);
            if (!resolved.IsSuccess)
                return Result.Fail<OperationsReport>(resolved.Error!);

            var period = resolved.Value;
            var start = period.StartUtc;
            var end = period.EndUtc;

            var cohort = await db
                .Orders.AsNoTracking()
                .Where(o => o.PlacedAt >= start && o.PlacedAt < end)
                .Select(o => new
                {
                    o.Status,
                    Paid = o.PaidAt ?? o.PlacedAt,
                    o.ProcessingAt,
                    o.ShippedAt,
                    o.DeliveredAt,
                    o.EstimatedDeliveryDate,
                })
                .ToListAsync(cancellationToken);

            var statuses = Enum.GetValues<OrderStatus>()
                .Select(status =>
                {
                    var count = cohort.Count(o => o.Status == status);
                    return new StatusCountRow(status, count, ReportMath.Share(count, cohort.Count));
                })
                .ToList();

            static List<double> Hours(IEnumerable<(DateTime? From, DateTime? To)> spans) =>
                spans
                    .Where(span => span.From is not null && span.To is not null)
                    .Select(span => (span.To!.Value - span.From!.Value).TotalHours)
                    .ToList();

            var stages = new (FulfillmentStage Stage, List<double> Hours)[]
            {
                (FulfillmentStage.Queue, Hours(cohort.Select(o => ((DateTime?)o.Paid, o.ProcessingAt)))),
                (FulfillmentStage.Preparation, Hours(cohort.Select(o => (o.ProcessingAt, o.ShippedAt)))),
                (FulfillmentStage.Transit, Hours(cohort.Select(o => (o.ShippedAt, o.DeliveredAt)))),
                (FulfillmentStage.Total, Hours(cohort.Select(o => ((DateTime?)o.Paid, o.DeliveredAt)))),
            }
                .Select(stage => new StageDurationRow(
                    stage.Stage,
                    stage.Hours.Count,
                    ReportMath.Mean(stage.Hours),
                    ReportMath.Median(stage.Hours)
                ))
                .ToList();

            var lateness = cohort
                .Where(o => o.DeliveredAt is not null)
                .Select(o => StoreCalendar.DateOf(o.DeliveredAt!.Value).DayNumber - o.EstimatedDeliveryDate.DayNumber)
                .ToList();
            var late = lateness.Where(days => days > 0).ToList();

            var onTime = new OnTimeSummary(
                lateness.Count,
                lateness.Count - late.Count,
                late.Count,
                lateness.Count == 0 ? null : ReportMath.Share(lateness.Count - late.Count, lateness.Count),
                late.Count == 0 ? null : Math.Round((decimal)late.Average(), 1)
            );

            var today = StoreCalendar.Today(clock);
            var overdue = await (
                from order in db.Orders.AsNoTracking()
                join client in db.Clients on order.ClientId equals client.ID
                where OpenStatuses.Contains(order.Status) && order.EstimatedDeliveryDate < today
                orderby order.EstimatedDeliveryDate, order.OrderNumber
                select new
                {
                    order.Id,
                    order.OrderNumber,
                    order.Status,
                    Customer = client.FirstName + " " + client.LastName,
                    order.PlacedAt,
                    order.EstimatedDeliveryDate,
                    order.Total,
                }
            ).ToListAsync(cancellationToken);

            return Result.Ok(
                new OperationsReport(
                    Meta(period),
                    cohort.Count,
                    statuses,
                    stages,
                    onTime,
                    overdue
                        .Select(o => new OverdueOrderRow(
                            o.Id,
                            o.OrderNumber,
                            o.Status,
                            o.Customer,
                            o.PlacedAt,
                            o.EstimatedDeliveryDate,
                            today.DayNumber - o.EstimatedDeliveryDate.DayNumber,
                            o.Total
                        ))
                        .ToList()
                )
            );
        }

        public async Task<Result<CancellationsReport>> CancellationsAsync(
            ReportQuery query,
            CancellationToken cancellationToken
        )
        {
            var resolved = Resolve(query);
            if (!resolved.IsSuccess)
                return Result.Fail<CancellationsReport>(resolved.Error!);

            var period = resolved.Value;
            var now = await PlacedTotalsAsync(period, cancellationToken);
            var then = await PlacedTotalsAsync(period.Previous, cancellationToken);

            var start = period.StartUtc;
            var end = period.EndUtc;

            var cancelled = await (
                from order in db.Orders.AsNoTracking()
                join client in db.Clients on order.ClientId equals client.ID
                where order.Status == OrderStatus.Cancelled && order.PlacedAt >= start && order.PlacedAt < end
                select new
                {
                    order.Id,
                    order.OrderNumber,
                    Customer = client.FirstName + " " + client.LastName,
                    client.UserId,
                    order.PlacedAt,
                    order.CancelledAt,
                    order.CancelReason,
                    order.Total,
                }
            ).ToListAsync(cancellationToken);

            var ids = cancelled.Select(o => o.Id.ToString(CultureInfo.InvariantCulture)).ToList();

            var entries = await db
                .AuditEntries.AsNoTracking()
                .Where(e =>
                    e.EntityType == AuditEntities.Order && e.Action == AuditActions.OrderCancelled && ids.Contains(e.EntityId)
                )
                .Select(e => new { e.EntityId, e.ActorUserId, e.Changes })
                .ToListAsync(cancellationToken);

            var trail = entries
                .GroupBy(e => e.EntityId)
                .ToDictionary(g => g.Key, g => g.First());

            var rows = cancelled
                .Select(order =>
                {
                    trail.TryGetValue(order.Id.ToString(CultureInfo.InvariantCulture), out var entry);

                    var actor = entry is null
                        ? CancellationActor.Unknown
                        : entry.ActorUserId == order.UserId
                            ? CancellationActor.Customer
                            : CancellationActor.Admin;

                    OrderStatus? stage =
                        entry is not null
                        && AuditChanges.Parse(entry.Changes).TryGetValue("status", out var change)
                        && Enum.TryParse<OrderStatus>(change.From, out var from)
                            ? from
                            : null;

                    return new CancelledOrderRow(
                        order.Id,
                        order.OrderNumber,
                        order.Customer,
                        order.PlacedAt,
                        order.CancelledAt,
                        actor,
                        stage,
                        order.CancelReason,
                        order.Total
                    );
                })
                .OrderByDescending(row => row.CancelledAt ?? row.PlacedAt)
                .ToList();

            var byActor = Enum.GetValues<CancellationActor>()
                .Select(actor =>
                {
                    var matching = rows.Where(row => row.CancelledBy == actor).ToList();
                    return new CancellationActorRow(
                        actor,
                        matching.Count,
                        matching.Sum(row => row.Total),
                        ReportMath.Share(matching.Count, rows.Count)
                    );
                })
                .Where(row => row.Actor != CancellationActor.Unknown || row.Orders > 0)
                .ToList();

            var byStage = new OrderStatus?[] { OrderStatus.Paid, OrderStatus.Processing, null }
                .Select(stage =>
                {
                    var count = rows.Count(row => row.Stage == stage);
                    return new CancellationStageRow(stage, count, ReportMath.Share(count, rows.Count));
                })
                .Where(row => row.Stage is not null || row.Orders > 0)
                .ToList();

            var reasons = rows
                .GroupBy(row => row.Reason?.Trim().ToLower(CultureInfo.CurrentCulture) ?? string.Empty)
                .Select(g => new CancellationReasonRow(
                    g.Key.Length == 0 ? null : g.OrderBy(row => row.CancelledAt).First().Reason!.Trim(),
                    g.Count(),
                    ReportMath.Share(g.Count(), rows.Count)
                ))
                .OrderByDescending(row => row.Orders)
                .ThenBy(row => row.Reason is null)
                .ThenBy(row => row.Reason, StringComparer.CurrentCulture)
                .Take(ReasonsLimit)
                .ToList();

            return Result.Ok(
                new CancellationsReport(
                    Meta(period),
                    ReportMath.Metric(now.Cancelled, then.Cancelled),
                    ReportMath.Metric(
                        ReportMath.Share(now.Cancelled, now.Placed),
                        ReportMath.Share(then.Cancelled, then.Placed)
                    ),
                    ReportMath.Metric(now.LostRevenue, then.LostRevenue),
                    byActor,
                    byStage,
                    reasons,
                    rows
                )
            );
        }

        public async Task<Result<InventoryReport>> InventoryAsync(ReportQuery query, CancellationToken cancellationToken)
        {
            var resolved = Resolve(query);
            if (!resolved.IsSuccess)
                return Result.Fail<InventoryReport>(resolved.Error!);

            var period = resolved.Value;

            var sold = await SoldLines(period)
                .GroupBy(line => line.ProductId)
                .Select(g => new { ProductId = g.Key, Units = g.Sum(line => line.Quantity) })
                .ToDictionaryAsync(row => row.ProductId, row => row.Units, cancellationToken);

            var active = (await Catalog(cancellationToken)).Values.Where(product => product.IsActive).ToList();

            var products = active
                .Select(product =>
                {
                    var unitsSold = sold.GetValueOrDefault(product.Id);
                    decimal? cover =
                        unitsSold == 0 ? null : Math.Round((decimal)product.Stock * period.Days / unitsSold, 1);

                    var level =
                        product.Stock == 0 ? StockLevel.OutOfStock
                        : product.Stock <= LowStockThreshold || cover <= CoverAlertDays ? StockLevel.Low
                        : StockLevel.Healthy;

                    return new StockRow(
                        product.Id,
                        product.Name,
                        product.Category,
                        product.Stock,
                        product.Price,
                        product.Stock * product.Price,
                        unitsSold,
                        Math.Round((decimal)unitsSold / period.Days, 2),
                        cover,
                        level
                    );
                })
                .OrderBy(row => row.Level)
                .ThenBy(row => row.DaysOfCover ?? decimal.MaxValue)
                .ThenBy(row => row.Name, StringComparer.CurrentCulture)
                .ToList();

            var value = products.Sum(row => row.Value);

            var categories = active
                .GroupBy(product => (product.CategoryId, product.Category))
                .Select(g =>
                {
                    var categoryValue = g.Sum(product => product.Stock * product.Price);
                    return new InventoryCategoryRow(
                        g.Key.CategoryId,
                        g.Key.Category,
                        g.Count(),
                        g.Sum(product => product.Stock),
                        categoryValue,
                        ReportMath.Share(categoryValue, value)
                    );
                })
                .OrderByDescending(row => row.Value)
                .ThenBy(row => row.Name, StringComparer.CurrentCulture)
                .ToList();

            return Result.Ok(
                new InventoryReport(
                    Meta(period),
                    new InventorySummary(
                        products.Count,
                        products.Count(row => row.Level == StockLevel.OutOfStock),
                        products.Count(row => row.Level == StockLevel.Low),
                        products.Count(row => row.Level == StockLevel.Healthy),
                        products.Sum(row => row.Stock),
                        value,
                        LowStockThreshold,
                        CoverAlertDays
                    ),
                    categories,
                    products
                )
            );
        }

        public async Task<Result<CustomersReport>> CustomersAsync(ReportQuery query, CancellationToken cancellationToken)
        {
            var resolved = Resolve(query);
            if (!resolved.IsSuccess)
                return Result.Fail<CustomersReport>(resolved.Error!);

            var period = resolved.Value;

            var firstOrders = await db
                .Orders.AsNoTracking()
                .Where(o => o.Status != OrderStatus.Cancelled)
                .GroupBy(o => o.ClientId)
                .Select(g => new { ClientId = g.Key, First = g.Min(o => o.PlacedAt) })
                .ToDictionaryAsync(row => row.ClientId, row => StoreCalendar.DateOf(row.First), cancellationToken);

            var current = await BuyerDaysAsync(period, cancellationToken);
            var previous = await BuyerDaysAsync(period.Previous, cancellationToken);

            BuyerTotals Summarize(ReportPeriod range, List<BuyerDay> days)
            {
                var buyers = days.Select(day => day.ClientId).Distinct().ToList();
                var fresh = buyers.Count(id =>
                    firstOrders.TryGetValue(id, out var first) && first >= range.From && first <= range.To
                );
                return new BuyerTotals(buyers.Count, fresh, days.Sum(day => day.Revenue));
            }

            var now = Summarize(period, current);
            var then = Summarize(period.Previous, previous);

            var series = period
                .Buckets()
                .Select(bucket =>
                {
                    var buyers = current
                        .Where(day => bucket.Contains(day.Day))
                        .Select(day => day.ClientId)
                        .Distinct()
                        .ToList();
                    var fresh = buyers.Count(id => firstOrders.TryGetValue(id, out var first) && bucket.Contains(first));
                    return new BuyersPoint(bucket.Start, bucket.End, fresh, buyers.Count - fresh);
                })
                .ToList();

            var orders = SalesIn(period);

            var top = await (
                from row in orders
                    .GroupBy(o => o.ClientId)
                    .Select(g => new
                    {
                        ClientId = g.Key,
                        Orders = g.Count(),
                        Revenue = g.Sum(o => o.Total),
                        LastOrderAt = g.Max(o => o.PlacedAt),
                    })
                join client in db.Clients on row.ClientId equals client.ID
                join user in db.Users on client.UserId equals user.Id
                orderby row.Revenue descending, row.Orders descending, row.ClientId
                select new
                {
                    row.ClientId,
                    Name = client.FirstName + " " + client.LastName,
                    Email = user.Email ?? string.Empty,
                    row.Orders,
                    row.Revenue,
                    row.LastOrderAt,
                }
            )
                .Take(TopCustomersLimit)
                .ToListAsync(cancellationToken);

            var provinces = await orders
                .GroupBy(o => o.ShipToProvince)
                .Select(g => new
                {
                    Name = g.Key,
                    Orders = g.Count(),
                    Buyers = g.Select(o => o.ClientId).Distinct().Count(),
                    Revenue = g.Sum(o => o.Total),
                })
                .OrderByDescending(row => row.Revenue)
                .ToListAsync(cancellationToken);

            var cities = await orders
                .GroupBy(o => new { o.ShipToCity, o.ShipToProvince })
                .Select(g => new
                {
                    g.Key.ShipToCity,
                    g.Key.ShipToProvince,
                    Orders = g.Count(),
                    Buyers = g.Select(o => o.ClientId).Distinct().Count(),
                    Revenue = g.Sum(o => o.Total),
                })
                .OrderByDescending(row => row.Revenue)
                .Take(CitiesLimit)
                .ToListAsync(cancellationToken);

            return Result.Ok(
                new CustomersReport(
                    Meta(period),
                    ReportMath.Metric(now.Buyers, then.Buyers),
                    ReportMath.Metric(now.New, then.New),
                    ReportMath.Metric(now.Buyers - now.New, then.Buyers - then.New),
                    ReportMath.Metric(
                        ReportMath.Average(now.Revenue, now.Buyers),
                        ReportMath.Average(then.Revenue, then.Buyers)
                    ),
                    series,
                    top.Select(row => new TopCustomerRow(
                            row.ClientId,
                            row.Name,
                            row.Email,
                            row.Orders,
                            row.Revenue,
                            ReportMath.Share(row.Revenue, now.Revenue),
                            row.LastOrderAt
                        ))
                        .ToList(),
                    provinces
                        .Select(row => new RegionRow(
                            row.Name,
                            null,
                            row.Orders,
                            row.Buyers,
                            row.Revenue,
                            ReportMath.Share(row.Revenue, now.Revenue)
                        ))
                        .ToList(),
                    cities
                        .Select(row => new RegionRow(
                            row.ShipToCity,
                            row.ShipToProvince,
                            row.Orders,
                            row.Buyers,
                            row.Revenue,
                            ReportMath.Share(row.Revenue, now.Revenue)
                        ))
                        .ToList()
                )
            );
        }

        public async Task<Result> RecordExportAsync(
            ReportExportRequest request,
            string actorUserId,
            CancellationToken cancellationToken
        )
        {
            var resolved = Resolve(new ReportQuery { From = request.From, To = request.To });
            if (!resolved.IsSuccess)
                return Result.Fail(resolved.Error!);

            var period = resolved.Value;
            var report = request.Report!.Value;
            var format = request.Format!.Value;

            var changes = new AuditChanges()
                .Set("format", format)
                .Set("from", period.From)
                .Set("to", period.To);
            if (!string.IsNullOrWhiteSpace(request.Table))
                changes.Set("table", request.Table.Trim());

            audit.Record(
                actorUserId,
                AuditActions.ReportExported,
                AuditEntities.Report,
                report,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Exportó «{Title(report)}» en {format.ToString().ToUpperInvariant()} ({period.From:dd/MM/yyyy} – {period.To:dd/MM/yyyy})"
                ),
                changes
            );

            await db.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }

        public static string Title(ReportKind report) =>
            report switch
            {
                ReportKind.Sales => "Ventas",
                ReportKind.Products => "Productos y categorías",
                ReportKind.Operations => "Operación y entregas",
                ReportKind.Cancellations => "Cancelaciones",
                ReportKind.Inventory => "Inventario",
                ReportKind.Customers => "Clientes y zonas",
                _ => "Informe completo",
            };

        private Result<ReportPeriod> Resolve(ReportQuery query) => ReportPeriod.Resolve(query, StoreCalendar.Today(clock));

        private ReportMeta Meta(ReportPeriod period) =>
            new(
                period.Range,
                period.Previous.Range,
                period.GroupBy,
                period.AvailableGroupings,
                StoreCalendar.Currency,
                StoreCalendar.TimeZoneId,
                clock.GetUtcNow().UtcDateTime
            );

        private IQueryable<Order> SalesIn(ReportPeriod period)
        {
            var start = period.StartUtc;
            var end = period.EndUtc;
            return db
                .Orders.AsNoTracking()
                .Where(o => o.Status != OrderStatus.Cancelled && o.PlacedAt >= start && o.PlacedAt < end);
        }

        private IQueryable<SoldLine> SoldLines(ReportPeriod period) =>
            from detail in db.OrderDetails.AsNoTracking()
            join order in SalesIn(period) on detail.OrderId equals order.Id
            select new SoldLine
            {
                OrderId = order.Id,
                ProductId = detail.ProductId,
                Quantity = detail.Quantity,
                Amount = detail.Quantity * detail.UnitPrice,
                PlacedAt = order.PlacedAt,
            };

        private async Task<List<DailySales>> DailySalesAsync(ReportPeriod period, CancellationToken cancellationToken)
        {
            var orders = await SalesIn(period)
                .GroupBy(o => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(o.PlacedAt, StoreCalendar.TimeZoneId).Date)
                .Select(g => new
                {
                    Day = g.Key,
                    Revenue = g.Sum(o => o.Total),
                    Orders = g.Count(),
                })
                .ToListAsync(cancellationToken);

            var units = await SoldLines(period)
                .GroupBy(line => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(line.PlacedAt, StoreCalendar.TimeZoneId).Date)
                .Select(g => new { Day = g.Key, Units = g.Sum(line => line.Quantity) })
                .ToDictionaryAsync(row => row.Day, row => row.Units, cancellationToken);

            return orders
                .Select(row => new DailySales(
                    DateOnly.FromDateTime(row.Day),
                    row.Revenue,
                    row.Orders,
                    units.GetValueOrDefault(row.Day)
                ))
                .ToList();
        }

        private static List<SalesBucket> Fold(ReportPeriod period, List<DailySales> days) =>
            period
                .Buckets()
                .Select(bucket =>
                {
                    var inside = days.Where(day => bucket.Contains(day.Day)).ToList();
                    return new SalesBucket(
                        bucket.Start,
                        bucket.End,
                        inside.Sum(day => day.Revenue),
                        inside.Sum(day => day.Orders),
                        inside.Sum(day => day.Units)
                    );
                })
                .ToList();

        private static SalesBucket Totals(List<SalesBucket> buckets) =>
            new(
                default,
                default,
                buckets.Sum(bucket => bucket.Revenue),
                buckets.Sum(bucket => bucket.Orders),
                buckets.Sum(bucket => bucket.Units)
            );

        private async Task<PlacedTotals> PlacedTotalsAsync(ReportPeriod period, CancellationToken cancellationToken)
        {
            var start = period.StartUtc;
            var end = period.EndUtc;

            var rows = await db
                .Orders.AsNoTracking()
                .Where(o => o.PlacedAt >= start && o.PlacedAt < end)
                .GroupBy(o => o.Status == OrderStatus.Cancelled)
                .Select(g => new
                {
                    Cancelled = g.Key,
                    Orders = g.Count(),
                    Total = g.Sum(o => o.Total),
                })
                .ToListAsync(cancellationToken);

            var cancelled = rows.FirstOrDefault(row => row.Cancelled);
            return new PlacedTotals(rows.Sum(row => row.Orders), cancelled?.Orders ?? 0, cancelled?.Total ?? 0);
        }

        private async Task<List<BuyerDay>> BuyerDaysAsync(ReportPeriod period, CancellationToken cancellationToken)
        {
            var rows = await SalesIn(period)
                .GroupBy(o => new
                {
                    o.ClientId,
                    Day = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(o.PlacedAt, StoreCalendar.TimeZoneId).Date,
                })
                .Select(g => new
                {
                    g.Key.ClientId,
                    g.Key.Day,
                    Revenue = g.Sum(o => o.Total),
                })
                .ToListAsync(cancellationToken);

            return rows.Select(row => new BuyerDay(row.ClientId, DateOnly.FromDateTime(row.Day), row.Revenue)).ToList();
        }

        private async Task<Dictionary<int, CatalogProduct>> Catalog(CancellationToken cancellationToken) =>
            await db
                .Products.AsNoTracking()
                .Select(p => new CatalogProduct(
                    p.Id,
                    p.Name,
                    p.ProductCategoryId,
                    p.Category.Name,
                    p.Price,
                    p.Stock,
                    p.IsActive,
                    p.CreatedAt
                ))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

        private sealed class SoldLine
        {
            public int OrderId { get; init; }
            public int ProductId { get; init; }
            public int Quantity { get; init; }
            public decimal Amount { get; init; }
            public DateTime PlacedAt { get; init; }
        }

        private sealed record DailySales(DateOnly Day, decimal Revenue, int Orders, int Units);

        private sealed record SalesBucket(DateOnly Start, DateOnly End, decimal Revenue, int Orders, int Units);

        private sealed record PlacedTotals(int Placed, int Cancelled, decimal LostRevenue);

        private sealed record BuyerDay(int ClientId, DateOnly Day, decimal Revenue);

        private sealed record BuyerTotals(int Buyers, int New, decimal Revenue);

        private sealed record CatalogProduct(
            int Id,
            string Name,
            int CategoryId,
            string Category,
            decimal Price,
            int Stock,
            bool IsActive,
            DateTime CreatedAt
        );
    }
}
