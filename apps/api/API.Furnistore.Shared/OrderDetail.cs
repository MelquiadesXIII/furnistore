using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Furnistore.Shared
{
    public class OrderDetail
    {
        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
    }
}