namespace API.Furnistore.Shared
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string TokenHash { get; set; } = string.Empty;

        public string JwtId { get; set; } = string.Empty;

        public bool IsUsed { get; set; }

        public bool IsRevoked { get; set; }

        public DateTime AddedDate { get; set; }

        public DateTime ExpiryDate { get; set; }
    }
}
