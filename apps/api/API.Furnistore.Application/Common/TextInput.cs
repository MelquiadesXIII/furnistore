namespace API.Furnistore.Application.Common
{
    internal static class TextInput
    {
        public static string? NullIfBlank(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
