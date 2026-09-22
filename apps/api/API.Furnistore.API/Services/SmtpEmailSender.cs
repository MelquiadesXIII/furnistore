using API.Furnistore.API.Configuration;
using API.Furnistore.Application.Auth;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace API.Furnistore.API.Services
{
    public sealed class SmtpEmailSender(IOptions<SmtpSettings> options) : IVerificationEmailSender
    {
        public async Task SendAsync(EmailMessage email, CancellationToken cancellationToken)
        {
            var settings = options.Value;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.SenderName, settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(email.To));
            message.Subject = email.Subject;
            message.Body = new BodyBuilder { HtmlBody = email.HtmlBody, TextBody = email.TextBody }.ToMessageBody();

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(settings.Timeout);

            using var client = new SmtpClient();
            await client.ConnectAsync(settings.Server, settings.Port, settings.Security, timeout.Token);

            if (settings.HasCredentials)
                await client.AuthenticateAsync(settings.UserName, settings.Password, timeout.Token);

            await client.SendAsync(message, timeout.Token);
            await client.DisconnectAsync(true, timeout.Token);
        }
    }
}
