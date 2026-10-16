using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using API.Furnistore.Application.Common;

namespace API.Furnistore.Application.Clients
{
    public sealed partial class PersonNameAttribute()
        : ValidationAttribute("Usa solo letras, separadas por un espacio, un guion o un apóstrofo.")
    {
        [GeneratedRegex(@"^[\p{L}\p{M}]+(?:[ '’-][\p{L}\p{M}]+)*$")]
        private static partial Regex Pattern();

        public override bool IsValid(object? value) =>
            value is null || (value is string text && Pattern().IsMatch(TextInput.CollapseSpaces(text)));
    }
}
