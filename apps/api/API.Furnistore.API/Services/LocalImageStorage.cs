using API.Furnistore.Application.Admin.Products;

namespace API.Furnistore.API.Services
{
    public sealed class LocalImageStorage(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
        : IProductImageStorage
    {
        public const string RequestPath = "/assets";

        public static string Folder(IHostEnvironment environment) =>
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "..", "assets"));

        public async Task<string> UploadAsync(
            string fileName,
            Stream content,
            string contentType,
            CancellationToken cancellationToken
        )
        {
            var request = httpContextAccessor.HttpContext?.Request
                ?? throw new InvalidOperationException("Solo se pueden subir imágenes dentro de una petición HTTP.");

            var folder = Folder(environment);
            Directory.CreateDirectory(folder);

            await using (var file = File.Create(Path.Combine(folder, fileName)))
                await content.CopyToAsync(file, cancellationToken);

            return $"{request.Scheme}://{request.Host}{RequestPath}/{fileName}";
        }
    }
}
