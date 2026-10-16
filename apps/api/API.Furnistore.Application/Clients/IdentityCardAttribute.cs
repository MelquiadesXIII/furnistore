using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace API.Furnistore.Application.Clients
{
    public sealed class IdentityCardAttribute()
        : ValidationAttribute("El carnet de identidad debe tener 11 dígitos y empezar por una fecha de nacimiento válida y no futura.")
    {
        public override bool IsValid(object? value) => value is null || (value is string text && IsValidNumber(text));

        public static bool IsValidNumber(string value)
        {
            if (value.Length != 11 || !value.All(char.IsAsciiDigit))
                return false;

            var century = value[6] switch
            {
                '9' => "18",
                >= '6' => "20",
                _ => "19",
            };

            return DateOnly.TryParseExact(
                    century + value[..6],
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var birthDate
                )
                && birthDate <= DateOnly.FromDateTime(DateTime.UtcNow);
        }
    }
}
