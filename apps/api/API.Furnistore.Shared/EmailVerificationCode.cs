namespace API.Furnistore.Shared
{
    public class EmailVerificationCode
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string CodeHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public int FailedAttempts { get; set; }
    }
}
