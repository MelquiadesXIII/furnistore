namespace API.Furnistore.Application.Auth
{
    public interface IVerificationEmailSender
    {
        Task SendAsync(string email, string subject, string htmlBody, CancellationToken cancellationToken);
    }

    public interface IEmailConfirmationLinkBuilder
    {
        string Build(string userId, string code);
    }
}
