namespace API.Furnistore.Shared
{
    public class Order
    {
        public int Id { get; set; }

        public int OrderNumber { get; set; }

        public int ClientId { get; set; }

        public OrderStatus Status { get; set; }

        public decimal Subtotal { get; set; }

        public decimal ShippingCost { get; set; }

        public decimal Total { get; set; }

        public string ShipToName { get; set; } = string.Empty;

        public string ShipToPhone { get; set; } = string.Empty;

        public string ShipToStreet { get; set; } = string.Empty;

        public string ShipToCity { get; set; } = string.Empty;

        public string ShipToProvince { get; set; } = string.Empty;

        public string? ShipToDeliveryNotes { get; set; }

        public DateTime PlacedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateOnly EstimatedDeliveryDate { get; set; }

        public DateTime? ProcessingAt { get; set; }

        public DateTime? ShippedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public string? CancelReason { get; set; }

        public List<OrderDetail> OrderDetails { get; set; } = [];
    }
}
