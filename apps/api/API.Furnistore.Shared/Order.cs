using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Furnistore.Shared
{
    public class Order
    {
        public int Id { get; set; }

        public int OrderNumber { get; set; }

        public int ClientId { get; set; }

        public DateTime OrderDate { get; set; }
        
        public DateTime DeliveryDate { get; set; }

        public OrderStatus Status { get; set; }

        public decimal Total { get; set; }

        public List<OrderDetail> OrderDetails { get; set; }
    }
}