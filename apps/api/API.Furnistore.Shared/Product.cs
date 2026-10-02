namespace API.Furnistore.Shared
{
    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public int ProductCategoryId { get; set; }

        public ProductCategory Category { get; set; } = null!;

        public string? ImageUrl { get; set; }

        public int? WidthCm { get; set; }

        public int? DepthCm { get; set; }

        public int? HeightCm { get; set; }

        public string? Material { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public uint Version { get; set; }
    }
}
