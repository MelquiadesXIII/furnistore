namespace API.Furnistore.Application.Orders
{
    public static class ShippingPolicy
    {
        public const int DeliveryLeadDays = 7;

        public static decimal CostFor(decimal subtotal) => 0m;
    }
}
