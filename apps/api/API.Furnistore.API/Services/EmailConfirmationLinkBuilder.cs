using API.Furnistore.API.Configuration;
using API.Furnistore.Application.Auth;
using Microsoft.Extensions.Options;

namespace API.Furnistore.API.Services
{
    public sealed class EmailConfirmationLinkBuilder(LinkGenerator links, IOptions<PublicUrls> urls)
        : IEmailConfirmationLinkBuilder
    {
        public string Build(string userId, string code)
        {
            var path =
                links.GetPathByAction(
                    action: "ConfirmEmail",
                    controller: "Authentication",
                    values: new { userId, code }
                ) ?? throw new InvalidOperationException("No se encontró la ruta de confirmación de correo.");

            return urls.Value.ApiUrl(path);
        }
    }
}
