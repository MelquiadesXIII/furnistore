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
        private const int TopCustomers = 10;

        public async Task<Result<SalesReport>> SalesAsync(ReportQuery query, CancellationToken cancellationToken)
        {
            var resolved = Resolve(query);
            if (!resolved.IsSuccess)
                return Result.Fail<SalesReport>(resolved.Error!);

            var period = resolved.Value;
            var start = period.StartUtc;
            var end = period.EndUtc;

            var days = await db
                .Orders.AsNoTracking()
                .Where(o => o.Status != OrderStatus.Cancelled && o.PlacedAt >= start && o.PlacedAt < end)
                .GroupBy(o => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(o.PlacedAt, StoreCalendar.TimeZoneId).Date)
                .Select(g => new
                {
                    Day = g.Key,
                    Revenue = g.Sum(o => o.Total),
                    Orders = g.Count(),
                    Units = g.Sum(o => o.OrderDetails.Sum(d => d.Quantity)),
                })
                .ToListAsync(cancellationToken);

            var series = period
                .Buckets()
                .Select(bucket =>
                {
                    var inside = days.Where(day => bucket.Contains(DateOnly.FromDateTime(day.Day))).ToList();
                    return new SalesPoint(
                        bucket.Start,
                        bucket.End,
                        inside.Sum(day => day.Revenue),
                        inside.Sum(day => day.Orders),
                        inside.Sum(day => day.Units)
                    );
                })
                .ToList();

            var revenue = series.Sum(point => point.Revenue);
            var orders = series.Sum(point => point.Orders);

            return Result.Ok(
                new SalesReport(
                    Meta(period),
                    revenue,
                    orders,
                    ReportMath.Average(revenue, orders),
                    series.Sum(point => point.Units),
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
            var start = period.StartUtc;
            var end = period.EndUtc;

            var sold = await (
                from detail in db.OrderDetails
                join order in db.Orders on detail.OrderId equals order.Id
                join product in db.Products on detail.ProductId equals product.Id
                where order.Status != OrderStatus.Cancelled && order.PlacedAt >= start && order.PlacedAt < end
                group detail by new { product.Id, product.Name, Category = product.Category.Name } into g
                select new
                {
                    g.Key.Id,
                    g.Key.Name,
                    g.Key.Category,
                    Units = g.Sum(d => d.Quantity),
                    Revenue = g.Sum(d => d.Quantity * d.UnitPrice),
                }
            ).ToListAsync(cancellationToken);

            var revenue = sold.Sum(row => row.Revenue);
            var soldIds = sold.Select(row => row.Id).ToList();

            var unsold = await db
                .Products.AsNoTracking()
                .CountAsync(p => p.IsActive && p.CreatedAt < end && !soldIds.Contains(p.Id), cancellationToken);

            var products = sold
                .OrderByDescending(row => row.Revenue)
                .ThenBy(row => row.Name)
                .Select(row => new ProductSalesRow(
                    row.Id,
                    row.Name,
                    row.Category,
                    row.Units,
                    row.Revenue,
                    ReportMath.Share(row.Revenue, revenue)
                ))
                .ToList();

            return Result.Ok(new ProductsReport(Meta(period), revenue, unsold, products));
        }

        public async Task<Result<OperationsReport>> OperationsAsync(ReportQuery query, CancellationToken cancellationToken)
        {
            var resolved = Resolve(query);
            if (!resolved.IsSuccess)
                return Result.Fail<OperationsReport>(resolved.Error!);

            var period = resolved.Value;
            var start = period.StartUtc;
            var end = period.EndUtc;

            var orders = await db
                .Orders.AsNoTracking()
                .Where(o => o.PlacedAt >= start && o.PlacedAt < end)
                .Select(o => new { o.Status, o.DeliveredAt, o.EstimatedDeliveryDate })
                .ToListAsync(cancellationToken);

            var statuses = Enum.GetValues<OrderStatus>()
                .Select(status => new StatusCountRow(status, orders.Count(o => o.Status == status)))
                .ToList();

            var delivered = orders.Where(o => o.DeliveredAt != null).ToList();
            var onTime = delivered.Count(o => StoreCalendar.DateOf(o.DeliveredAt!.Value) <= o.EstimatedDeliveryDate);

            var today = StoreCalendar.Today(clock);
            var open = new[] { OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped };

            var overdue = await (
                from order in db.Orders.AsNoTracking()
                join client in db.Clients on order.ClientId equals client.ID
                where open.Contains(order.Status) && order.EstimatedDeliveryDate < today
                orderby order.EstimatedDeliveryDate
                select new
                {
                    order.Id,
                    order.OrderNumber,
                    order.Status,
                    Customer = client.FirstName + " " + client.LastName,
                    order.EstimatedDeliveryDate,
                    order.Total,
                }
            ).ToListAsync(cancellationToken);

            return Result.Ok(
                new OperationsReport(
                    Meta(period),
                    orders.Count,
                    delivered.Count,
                    onTime,
                    delivered.Count == 0 ? null : ReportMath.Share(onTime, delivered.Count),
                    statuses,
                    overdue
                        .Select(o => new OverdueOrderRow(
                            o.Id,
                            o.OrderNumber,
                            o.Status,
                            o.Customer,
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
            var start = period.StartUtc;
            var end = period.EndUtc;

            var placed = await db
                .Orders.AsNoTracking()
                .CountAsync(o => o.PlacedAt >= start && o.PlacedAt < end, cancellationToken);

            var cancelled = await (
                from order in db.Orders.AsNoTracking()
                join client in db.Clients on order.ClientId equals client.ID
                where order.Status == OrderStatus.Cancelled && order.PlacedAt >= start && order.PlacedAt < end
                orderby order.CancelledAt descending
                select new
                {
                    order.Id,
                    order.OrderNumber,
                    Customer = client.FirstName + " " + client.LastName,
                    client.UserId,
                    order.CancelledAt,
                    order.CancelReason,
                    order.Total,
                }
            ).ToListAsync(cancellationToken);

            var ids = cancelled.Select(o => o.Id.ToString(CultureInfo.InvariantCulture)).ToList();

            var actors = await db
                .AuditEntries.AsNoTracking()
                .Where(e => e.Action == AuditActions.OrderCancelled && ids.Contains(e.EntityId))
                .Select(e => new { e.EntityId, e.ActorUserId })
                .ToListAsync(cancellationToken);

            var rows = cancelled
                .Select(order =>
                {
                    var actor = actors.FirstOrDefault(a => a.EntityId == order.Id.ToString(CultureInfo.InvariantCulture));
                    var cancelledBy =
                        actor == null ? CancellationActor.Unknown
                        : actor.ActorUserId == order.UserId ? CancellationActor.Customer
                        : CancellationActor.Admin;

                    return new CancelledOrderRow(
                        order.Id,
                        order.OrderNumber,
                        order.Customer,
                        order.CancelledAt,
                        cancelledBy,
                        order.CancelReason,
                        order.Total
                    );
                })
                .ToList();

            var byActor = Enum.GetValues<CancellationActor>()
                .Select(actor =>
                {
                    var count = rows.Count(row => row.CancelledBy == actor);
                    return new CancellationActorRow(actor, count, ReportMath.Share(count, rows.Count));
                })
                .Where(row => row.Orders > 0)
                .ToList();

            return Result.Ok(
                new CancellationsReport(
                    Meta(period),
                    rows.Count,
                    ReportMath.Share(rows.Count, placed),
                    rows.Sum(row => row.Total),
                    byActor,
                    rows
                )
            );
        }

        public async Task<Result<InventoryReport>> InventoryAsync(ReportQuery query, CancellationToken cancellationToken)
        {
            var resolved = Resolve(query);
            if (!resolved.IsSuccess)
                return Result.Fail<InventoryReport>(resolved.Error!);

            var products = await db
                .Products.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Stock)
                .ThenBy(p => p.Name)
                .Select(p => new StockRow(
                    p.Id,
                    p.Name,
                    p.Category.Name,
                    p.Stock,
                    p.Price,
                    p.Stock * p.Price,
                    p.Stock == 0 ? StockLevel.OutOfStock
                    : p.Stock <= LowStockThreshold ? StockLevel.Low
                    : StockLevel.Healthy
                ))
                .ToListAsync(cancellationToken);

            return Result.Ok(
                new InventoryReport(
                    Meta(resolved.Value),
                    products.Count,
                    products.Count(p => p.Level == StockLevel.OutOfStock),
                    products.Count(p => p.Level == StockLevel.Low),
                    products.Count(p => p.Level == StockLevel.Healthy),
                    products.Sum(p => p.Value),
                    LowStockThreshold,
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
            var start = period.StartUtc;
            var end = period.EndUtc;

            var orders = db
                .Orders.AsNoTracking()
                .Where(o => o.Status != OrderStatus.Cancelled && o.PlacedAt >= start && o.PlacedAt < end);

            var buyerIds = await orders.Select(o => o.ClientId).Distinct().ToListAsync(cancellationToken);

            var newBuyers = await db
                .Orders.AsNoTracking()
                .Where(o => o.Status != OrderStatus.Cancelled && buyerIds.Contains(o.ClientId))
                .GroupBy(o => o.ClientId)
                .CountAsync(g => g.Min(o => o.PlacedAt) >= start, cancellationToken);

            var provinces = await orders
                .GroupBy(o => o.ShipToProvince)
                .Select(g => new { Name = g.Key, Orders = g.Count(), Revenue = g.Sum(o => o.Total) })
                .OrderByDescending(row => row.Revenue)
                .ToListAsync(cancellationToken);

            var topCustomers = await (
                from order in orders
                join client in db.Clients on order.ClientId equals client.ID
                join user in db.Users on client.UserId equals user.Id
                group order by new { client.ID, client.FirstName, client.LastName, user.Email } into g
                orderby g.Sum(o => o.Total) descending
                select new TopCustomerRow(
                    g.Key.ID,
                    g.Key.FirstName + " " + g.Key.LastName,
                    g.Key.Email ?? string.Empty,
                    g.Count(),
                    g.Sum(o => o.Total)
                )
            )
                .Take(TopCustomers)
                .ToListAsync(cancellationToken);

            return Result.Ok(
                new CustomersReport(
                    Meta(period),
                    buyerIds.Count,
                    newBuyers,
                    buyerIds.Count - newBuyers,
                    provinces.Select(row => new ProvinceRow(row.Name, row.Orders, row.Revenue)).ToList(),
                    topCustomers
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
                ReportKind.Products => "Productos",
                ReportKind.Operations => "Operación",
                ReportKind.Cancellations => "Cancelaciones",
                ReportKind.Inventory => "Inventario",
                _ => "Clientes",
            };

        private Result<ReportPeriod> Resolve(ReportQuery query) => ReportPeriod.Resolve(query, StoreCalendar.Today(clock));

        private ReportMeta Meta(ReportPeriod period) =>
            new(
                period.Range,
                period.GroupBy,
                period.AvailableGroupings,
                StoreCalendar.Currency,
                clock.GetUtcNow().UtcDateTime
            );
    }
}
