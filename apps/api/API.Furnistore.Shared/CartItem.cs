namespace API.Furnistore.Shared
{
    public class CartItem
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }
}
