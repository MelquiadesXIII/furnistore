namespace API.Furnistore.API.Configuration
{
    public class SupabaseStorageSettings
    {
        public string Url { get; set; } = string.Empty;

        public string ServiceKey { get; set; } = string.Empty;

        public string Bucket { get; set; } = "products";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(ServiceKey);
    }
}
