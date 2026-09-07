using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Furnistore.Shared
{
    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public int ProductCategoryId { get; set; }

        public string? ImageUrl { get; set; }

        public List<OrderDetail> OrderDetails { get; set; }
    }
}