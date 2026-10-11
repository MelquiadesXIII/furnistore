namespace API.Furnistore.Application.Admin.Products
{
    public interface IProductImageStorage
    {
        Task<string> UploadAsync(string fileName, Stream content, string contentType, CancellationToken cancellationToken);
    }
}
