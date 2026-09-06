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

    public sealed record CreateClientRequest : IValidatableObject
    {
        [Required, StringLength(60, MinimumLength = 2)]
        public required string FirstName { get; init; }

        [Required, StringLength(60, MinimumLength = 2)]
        public required string LastName { get; init; }

        public DateTime BirthDate { get; init; }

        [Required, RegularExpression(@"^\+?[1-9]\d{6,14}$")]
        public required string Phone { get; init; }

        [Required, StringLength(200, MinimumLength = 5)]
        public required string Address { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            ClientRules.ValidateBirthDate(BirthDate);
    }

    public sealed record UpdateClientRequest : IValidatableObject
    {
        [Required, StringLength(60, MinimumLength = 2)]
        public required string FirstName { get; init; }

        [Required, StringLength(60, MinimumLength = 2)]
        public required string LastName { get; init; }

        public DateTime BirthDate { get; init; }

        [Required, RegularExpression(@"^\+?[1-9]\d{6,14}$")]
        public required string Phone { get; init; }

        [Required, StringLength(200, MinimumLength = 5)]
        public required string Address { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            ClientRules.ValidateBirthDate(BirthDate);
    }

    internal static class ClientRules
    {
        public static IEnumerable<ValidationResult> ValidateBirthDate(DateTime birthDate)
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age))
                age--;

            if (birthDate.Date >= today)
                yield return new ValidationResult(
                    "birthDate debe ser una fecha pasada.",
                    new[] { nameof(CreateClientRequest.BirthDate) }
                );
            else if (age < 18 || age > 120)
                yield return new ValidationResult(
                    "La edad debe estar entre 18 y 120 años.",
                    new[] { nameof(CreateClientRequest.BirthDate) }
                );
        }
    }

    public sealed record ClientResponse(
        int Id,
        string FirstName,
        string LastName,
        DateTime BirthDate,
        string Phone,
        string Address
    );
}
