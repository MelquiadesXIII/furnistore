using API.Furnistore.Application.Auth;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace API.Furnistore.API.Services
{
    public sealed class IdentityVerificationEmailSender(IEmailSender emailSender)
        : IVerificationEmailSender
    {
        public Task SendAsync(
            string email,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken
        ) => emailSender.SendEmailAsync(email, subject, htmlBody);
    }

    public sealed class EmailConfirmationLinkBuilder(
        IHttpContextAccessor httpContextAccessor,
        IUrlHelperFactory urlHelperFactory,
        IActionContextAccessor actionContextAccessor
    ) : IEmailConfirmationLinkBuilder
    {
        public string Build(string userId, string code)
        {
            var request = httpContextAccessor.HttpContext?.Request;
            var actionContext = actionContextAccessor.ActionContext;

            if (request is null || actionContext is null)
                return string.Empty;

            var url = urlHelperFactory.GetUrlHelper(actionContext);
            var path = url.Action("ConfirmEmail", "Authentication", new { userId, code });

            return $"{request.Scheme}://{request.Host}{path}";
        }
    }
}
