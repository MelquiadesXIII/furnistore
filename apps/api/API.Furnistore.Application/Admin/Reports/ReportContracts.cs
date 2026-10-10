using System.ComponentModel.DataAnnotations;
using API.Furnistore.Shared;

namespace API.Furnistore.Application.Admin.Reports
{
    public enum ReportGrouping
    {
        Day,
        Week,
        Month,
    }

    public enum ReportKind
    {
        Sales,
        Products,
        Operations,
        Cancellations,
        Inventory,
        Customers,
    }

    public enum ReportFormat
    {
        Pdf,
        Csv,
    }

    public sealed record ReportQuery
    {
        public DateOnly? From { get; init; }

        public DateOnly? To { get; init; }

        public ReportGrouping? GroupBy { get; init; }
    }

    public sealed record ReportExportRequest
    {
        [Required]
        public ReportKind? Report { get; init; }

        [Required]
        public ReportFormat? Format { get; init; }

        [StringLength(60)]
        public string? Table { get; init; }

        public DateOnly? From { get; init; }

        public DateOnly? To { get; init; }
    }

    public sealed record ReportRange(DateOnly From, DateOnly To);

    public sealed record ReportMeta(
        ReportRange Period,
        ReportGrouping GroupBy,
        IReadOnlyList<ReportGrouping> AvailableGroupings,
        string Currency,
        DateTime GeneratedAt
    );

    public sealed record SalesReport(
        ReportMeta Meta,
        decimal Revenue,
        int Orders,
        decimal AverageOrderValue,
        int Units,
        IReadOnlyList<SalesPoint> Series
    );

    public sealed record SalesPoint(DateOnly Start, DateOnly End, decimal Revenue, int Orders, int Units);

    public sealed record ProductsReport(
        ReportMeta Meta,
        decimal Revenue,
        int UnsoldProducts,
        IReadOnlyList<ProductSalesRow> Products
    );

    public sealed record ProductSalesRow(int ProductId, string Name, string Category, int Units, decimal Revenue, decimal Share);

    public sealed record OperationsReport(
        ReportMeta Meta,
        int Orders,
        int Delivered,
        int DeliveredOnTime,
        decimal? OnTimeRate,
        IReadOnlyList<StatusCountRow> Statuses,
        IReadOnlyList<OverdueOrderRow> Overdue
    );

    public sealed record StatusCountRow(OrderStatus Status, int Orders);

    public sealed record OverdueOrderRow(
        int OrderId,
        int OrderNumber,
        OrderStatus Status,
        string Customer,
        DateOnly EstimatedDeliveryDate,
        int DaysLate,
        decimal Total
    );

    public enum CancellationActor
    {
        Customer,
        Admin,
        Unknown,
    }

    public sealed record CancellationsReport(
        ReportMeta Meta,
        int Cancelled,
        decimal Rate,
        decimal LostRevenue,
        IReadOnlyList<CancellationActorRow> ByActor,
        IReadOnlyList<CancelledOrderRow> Orders
    );

    public sealed record CancellationActorRow(CancellationActor Actor, int Orders, decimal Share);

    public sealed record CancelledOrderRow(
        int OrderId,
        int OrderNumber,
        string Customer,
        DateTime? CancelledAt,
        CancellationActor CancelledBy,
        string? Reason,
        decimal Total
    );

    public enum StockLevel
    {
        OutOfStock,
        Low,
        Healthy,
    }

    public sealed record InventoryReport(
        ReportMeta Meta,
        int ActiveProducts,
        int OutOfStock,
        int LowStock,
        int Healthy,
        decimal Value,
        int LowStockThreshold,
        IReadOnlyList<StockRow> Products
    );

    public sealed record StockRow(
        int ProductId,
        string Name,
        string Category,
        int Stock,
        decimal Price,
        decimal Value,
        StockLevel Level
    );

    public sealed record CustomersReport(
        ReportMeta Meta,
        int Buyers,
        int NewBuyers,
        int ReturningBuyers,
        IReadOnlyList<ProvinceRow> Provinces,
        IReadOnlyList<TopCustomerRow> TopCustomers
    );

    public sealed record ProvinceRow(string Name, int Orders, decimal Revenue);

    public sealed record TopCustomerRow(int CustomerId, string Name, string Email, int Orders, decimal Revenue);
}
