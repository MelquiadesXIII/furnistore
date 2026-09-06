using System.Security.Claims;

namespace API.Furnistore.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string UserId(this ClaimsPrincipal principal) =>
            principal.FindFirst("Id")?.Value ?? "anonymous";
    }
}
