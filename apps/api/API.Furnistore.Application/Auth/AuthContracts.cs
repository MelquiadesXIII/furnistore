using System.ComponentModel.DataAnnotations;

namespace API.Furnistore.Application.Auth
{
    public sealed record RegisterRequest
    {
        [Required, StringLength(80, MinimumLength = 2)]
        public required string Name { get; init; }

        [Required, EmailAddress, StringLength(256)]
        public required string EmailAddress { get; init; }

        [Required, StringLength(128, MinimumLength = 8)]
        [RegularExpression(@".*\d.*", ErrorMessage = "La contraseña debe incluir al menos un dígito.")]
        public required string Password { get; init; }
    }

    public sealed record LoginRequest
    {
        [Required, EmailAddress, StringLength(256)]
        public required string Email { get; init; }

        [Required, StringLength(128)]
        public required string Password { get; init; }
    }

    public sealed record RefreshTokenRequest
    {
        [Required]
        public required string Token { get; init; }

        [Required]
        public required string RefreshToken { get; init; }
    }

    public sealed record AuthTokensResponse(string Token, string RefreshToken);

    public sealed record RegisterResponse(bool EmailSent);
}
