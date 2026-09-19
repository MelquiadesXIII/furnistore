namespace API.Furnistore.Shared
{
    public class Client
    {
        public int ID { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Street { get; set; }

        public string? City { get; set; }

        public string? Province { get; set; }

        public string? DeliveryNotes { get; set; }

        public bool IsProfileComplete =>
            !string.IsNullOrWhiteSpace(Phone)
            && !string.IsNullOrWhiteSpace(Street)
            && !string.IsNullOrWhiteSpace(City)
            && !string.IsNullOrWhiteSpace(Province);
    }
}
