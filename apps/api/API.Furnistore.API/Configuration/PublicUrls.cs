namespace API.Furnistore.API.Configuration
{
    public sealed class PublicUrls
    {
        public string Api { get; set; } = string.Empty;

        public string Frontend { get; set; } = string.Empty;

        public bool IsValid => IsAbsoluteHttpUrl(Api) && IsAbsoluteHttpUrl(Frontend);

        public string ApiUrl(string path) => $"{Api.TrimEnd('/')}{path}";

        public string FrontendUrl(string path) => $"{Frontend.TrimEnd('/')}{path}";

        private static bool IsAbsoluteHttpUrl(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
