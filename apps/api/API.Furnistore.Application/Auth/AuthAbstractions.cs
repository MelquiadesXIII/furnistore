namespace API.Furnistore.Application.Auth
{
    public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);

    public interface IVerificationEmailSender
    {
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
    }

    public interface IEmailConfirmationLinkBuilder
    {
        string Build(string userId, string code);
    }
}
