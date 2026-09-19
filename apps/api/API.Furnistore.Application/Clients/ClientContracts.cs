using System.ComponentModel.DataAnnotations;

namespace API.Furnistore.Application.Clients
{
    public sealed record ClientQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 20;

        [StringLength(120)]
        public string? Search { get; init; }
    }

    public sealed record ShippingAddress
    {
        [Required, StringLength(200, MinimumLength = 3)]
        public required string Street { get; init; }

        [Required, StringLength(100, MinimumLength = 2)]
        public required string City { get; init; }

        [Required, StringLength(100, MinimumLength = 2)]
        public required string Province { get; init; }

        [StringLength(300)]
        public string? DeliveryNotes { get; init; }
    }

    public sealed record UpdateClientRequest
    {
        [Required, StringLength(60, MinimumLength = 2)]
        public required string FirstName { get; init; }

        [Required, StringLength(60, MinimumLength = 2)]
        public required string LastName { get; init; }

        [Required, RegularExpression(@"^\+?[1-9]\d{6,14}$")]
        public required string Phone { get; init; }

        [Required]
        public required ShippingAddress Address { get; init; }
    }

    public sealed record ClientResponse(
        int Id,
        string Email,
        string FirstName,
        string LastName,
        string? Phone,
        ShippingAddress? Address,
        bool IsProfileComplete
    );
}
