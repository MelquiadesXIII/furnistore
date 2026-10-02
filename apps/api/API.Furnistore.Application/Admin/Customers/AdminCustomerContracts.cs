using System.ComponentModel.DataAnnotations;
using API.Furnistore.Application.Admin.Audit;
using API.Furnistore.Application.Admin.Orders;
using API.Furnistore.Application.Clients;

namespace API.Furnistore.Application.Admin.Customers
{
    public enum CustomerFilter
    {
        All,
        Admins,
        Disabled,
        LockedOut,
        Unconfirmed,
    }

    public sealed record AdminCustomerQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 20;

        [StringLength(120)]
        public string? Search { get; init; }

        public CustomerFilter Filter { get; init; } = CustomerFilter.All;

        [StringLength(40), RegularExpression("^-?[A-Za-z]+$")]
        public string? Sort { get; init; }
    }

    public sealed record UpdateCustomerRequest
    {
        [Required, StringLength(60, MinimumLength = 2)]
        public required string FirstName { get; init; }

        [Required, StringLength(60, MinimumLength = 2)]
        public required string LastName { get; init; }

        [Required, RegularExpression(@"^\+?[1-9]\d{6,14}$")]
        public required string Phone { get; init; }

        [Required]
        public required ShippingAddress Address { get; init; }

        public uint Version { get; init; }
    }

    public sealed record AdminCustomerSummary(
        int Id,
        string FirstName,
        string LastName,
        string Email,
        string? Phone,
        bool EmailConfirmed,
        bool IsAdmin,
        bool IsDisabled,
        bool IsLockedOut,
        int OrderCount
    );

    public sealed record AdminAccountStatus(
        bool EmailConfirmed,
        bool IsAdmin,
        bool IsDisabled,
        DateTimeOffset? LockedOutUntil,
        int FailedLoginCount
    );

    public sealed record AdminCartLine(int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

    public sealed record AdminCustomerResponse(
        int Id,
        string FirstName,
        string LastName,
        string Email,
        string? Phone,
        ShippingAddress? Address,
        bool IsProfileComplete,
        uint Version,
        AdminAccountStatus Account,
        int OrderCount,
        decimal TotalSpent,
        IReadOnlyList<AdminCartLine> Cart,
        IReadOnlyList<AdminOrderSummary> RecentOrders,
        IReadOnlyList<AuditEntryResponse> History
    );
}
