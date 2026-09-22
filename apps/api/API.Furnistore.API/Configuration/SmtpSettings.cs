using MailKit.Security;

namespace API.Furnistore.API.Configuration
{
    public class SmtpSettings
    {
        public string Server { get; set; } = string.Empty;

        public int Port { get; set; }

        public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

        public string SenderEmail { get; set; } = string.Empty;

        public string SenderName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool HasCredentials =>
            !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password);
    }
}
