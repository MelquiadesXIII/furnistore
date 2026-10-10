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
        All,
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
        ReportRange PreviousPeriod,
        ReportGrouping GroupBy,
        IReadOnlyList<ReportGrouping> AvailableGroupings,
        string Currency,
        string TimeZone,
        DateTime GeneratedAt
    );

    public sealed record ReportMetric(decimal Value, decimal Previous, decimal? Change);

    public sealed record SalesReport(ReportMeta Meta, SalesSummary Summary, IReadOnlyList<SalesPoint> Series);

    public sealed record SalesSummary(
        ReportMetric Revenue,
        ReportMetric Orders,
        ReportMetric AverageOrderValue,
        ReportMetric Units
    );

    public sealed record SalesPoint(
        DateOnly Start,
        DateOnly End,
        decimal Revenue,
        int Orders,
        int Units,
        decimal AverageOrderValue,
        decimal? PreviousRevenue,
        int? PreviousOrders
    );

    public sealed record ProductsReport(
        ReportMeta Meta,
        decimal Revenue,
        IReadOnlyList<ProductSalesRow> Products,
        IReadOnlyList<CategorySalesRow> Categories,
        IReadOnlyList<CategorySeriesPoint> CategorySeries,
        IReadOnlyList<UnsoldProductRow> Unsold
    );

    public sealed record ProductSalesRow(
        int ProductId,
        string Name,
        string Category,
        int Units,
        int Orders,
        decimal Revenue,
        decimal Share,
        decimal AverageUnitPrice
    );

    public sealed record CategorySalesRow(int CategoryId, string Name, int Units, int Orders, decimal Revenue, decimal Share);

    public sealed record CategoryAmount(int CategoryId, decimal Revenue);

    public sealed record CategorySeriesPoint(DateOnly Start, DateOnly End, IReadOnlyList<CategoryAmount> Categories);

    public sealed record UnsoldProductRow(
        int ProductId,
        string Name,
        string Category,
        decimal Price,
        int Stock,
        DateTime? LastSoldAt
    );

    public enum FulfillmentStage
    {
        Queue,
        Preparation,
        Transit,
        Total,
    }

    public sealed record OperationsReport(
        ReportMeta Meta,
        int Orders,
        IReadOnlyList<StatusCountRow> Statuses,
        IReadOnlyList<StageDurationRow> Stages,
        OnTimeSummary OnTime,
        IReadOnlyList<OverdueOrderRow> Overdue
    );

    public sealed record StatusCountRow(OrderStatus Status, int Orders, decimal Share);

    public sealed record StageDurationRow(FulfillmentStage Stage, int Orders, decimal? AverageHours, decimal? MedianHours);

    public sealed record OnTimeSummary(int Delivered, int OnTime, int Late, decimal? OnTimeRate, decimal? AverageDaysLate);

    public sealed record OverdueOrderRow(
        int OrderId,
        int OrderNumber,
        OrderStatus Status,
        string Customer,
        DateTime PlacedAt,
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
        ReportMetric Cancelled,
        ReportMetric Rate,
        ReportMetric LostRevenue,
        IReadOnlyList<CancellationActorRow> ByActor,
        IReadOnlyList<CancellationStageRow> ByStage,
        IReadOnlyList<CancellationReasonRow> Reasons,
        IReadOnlyList<CancelledOrderRow> Orders
    );

    public sealed record CancellationActorRow(CancellationActor Actor, int Orders, decimal Revenue, decimal Share);

    public sealed record CancellationStageRow(OrderStatus? Stage, int Orders, decimal Share);

    public sealed record CancellationReasonRow(string? Reason, int Orders, decimal Share);

    public sealed record CancelledOrderRow(
        int OrderId,
        int OrderNumber,
        string Customer,
        DateTime PlacedAt,
        DateTime? CancelledAt,
        CancellationActor CancelledBy,
        OrderStatus? Stage,
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
        InventorySummary Summary,
        IReadOnlyList<InventoryCategoryRow> Categories,
        IReadOnlyList<StockRow> Products
    );

    public sealed record InventorySummary(
        int ActiveProducts,
        int OutOfStock,
        int LowStock,
        int Healthy,
        int Units,
        decimal Value,
        int LowStockThreshold,
        int CoverAlertDays
    );

    public sealed record InventoryCategoryRow(int CategoryId, string Name, int Products, int Units, decimal Value, decimal Share);

    public sealed record StockRow(
        int ProductId,
        string Name,
        string Category,
        int Stock,
        decimal Price,
        decimal Value,
        int UnitsSold,
        decimal DailyUnits,
        decimal? DaysOfCover,
        StockLevel Level
    );

    public sealed record CustomersReport(
        ReportMeta Meta,
        ReportMetric Buyers,
        ReportMetric NewBuyers,
        ReportMetric ReturningBuyers,
        ReportMetric RevenuePerBuyer,
        IReadOnlyList<BuyersPoint> Series,
        IReadOnlyList<TopCustomerRow> TopCustomers,
        IReadOnlyList<RegionRow> Provinces,
        IReadOnlyList<RegionRow> Cities
    );

    public sealed record BuyersPoint(DateOnly Start, DateOnly End, int NewBuyers, int ReturningBuyers);

    public sealed record TopCustomerRow(
        int CustomerId,
        string Name,
        string Email,
        int Orders,
        decimal Revenue,
        decimal Share,
        DateTime LastOrderAt
    );

    public sealed record RegionRow(string Name, string? Province, int Orders, int Buyers, decimal Revenue, decimal Share);
}
