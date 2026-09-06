using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Furnistore.API.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace API.Furnistore.API.Services
{
    public class EmailService : IEmailSender
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail)
            );
            message.To.Add(new MailboxAddress("", email));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlMessage };

            using (var client = new SmtpClient { Timeout = 15000 })
            {
                await client.ConnectAsync(
                    _smtpSettings.Server,
                    _smtpSettings.Port,
                    SecureSocketOptions.StartTls
                );
                await client.AuthenticateAsync(_smtpSettings.UserName, _smtpSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}
