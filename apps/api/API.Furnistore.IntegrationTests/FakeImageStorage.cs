using API.Furnistore.Application.Admin.Products;

namespace API.Furnistore.IntegrationTests
{
    public sealed class FakeImageStorage : IProductImageStorage
    {
        public bool Fail { get; set; }

        public List<string> Uploaded { get; } = [];

        public Task<string> UploadAsync(string fileName, Stream content, string contentType, CancellationToken cancellationToken)
        {
            if (Fail)
                throw new HttpRequestException("Storage caído");

            Uploaded.Add(fileName);
            return Task.FromResult($"https://storage.test/products/{fileName}");
        }
    }
}
