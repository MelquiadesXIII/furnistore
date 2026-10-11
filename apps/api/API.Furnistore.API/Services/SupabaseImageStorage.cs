using System.Net.Http.Headers;
using API.Furnistore.API.Configuration;
using API.Furnistore.Application.Admin.Products;
using Microsoft.Extensions.Options;

namespace API.Furnistore.API.Services
{
    public sealed class SupabaseImageStorage(HttpClient http, IOptions<SupabaseStorageSettings> options)
        : IProductImageStorage
    {
        public async Task<string> UploadAsync(
            string fileName,
            Stream content,
            string contentType,
            CancellationToken cancellationToken
        )
        {
            var settings = options.Value;
            if (!settings.IsConfigured)
                throw new HttpRequestException("Supabase Storage no está configurado.");

            var baseUrl = settings.Url.TrimEnd('/');

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl}/storage/v1/object/{settings.Bucket}/{fileName}"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ServiceKey);
            request.Headers.Add("apikey", settings.ServiceKey);
            request.Content = new StreamContent(content);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            using var response = await http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            return $"{baseUrl}/storage/v1/object/public/{settings.Bucket}/{fileName}";
        }
    }
}
