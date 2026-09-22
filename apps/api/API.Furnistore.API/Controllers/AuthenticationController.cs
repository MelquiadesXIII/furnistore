using API.Furnistore.API.Configuration;
using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Auth;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace API.Furnistore.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    // Lo de abajo es para limitar el numero de intentos por minuto de un usaurio...
    [EnableRateLimiting(AuthRateLimits.Session)]
    [Route("api/authentication")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public sealed class AuthenticationController(AuthService auth, IOptions<PublicUrls> urls) : ControllerBase
    {
        [HttpPost("register")]
        [EnableRateLimiting(AuthRateLimits.Credentials)]
        [ProducesResponseType<RegisterResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(
            RegisterRequest request,
            CancellationToken cancellationToken
        ) => (await auth.RegisterAsync(request, cancellationToken)).ToActionResult(this);

        [HttpPost("login")]
        [EnableRateLimiting(AuthRateLimits.Credentials)]
        [ProducesResponseType<AuthTokensResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            LoginRequest request,
            CancellationToken cancellationToken
        ) => (await auth.LoginAsync(request, cancellationToken)).ToActionResult(this);

        [HttpPost("resend-confirmation")]
        [EnableRateLimiting(AuthRateLimits.Credentials)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendConfirmation(
            ResendConfirmationRequest request,
            CancellationToken cancellationToken
        )
        {
            await auth.ResendConfirmationAsync(request, cancellationToken);
            return Accepted();
        }

        [HttpPost("refresh-token")]
        [ProducesResponseType<AuthTokensResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken(
            RefreshTokenRequest request,
            CancellationToken cancellationToken
        ) => (await auth.RefreshAsync(request, cancellationToken)).ToActionResult(this);

        [HttpGet("confirm-email", Name = "ConfirmEmail")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmEmail(
            [FromQuery] string? userId,
            [FromQuery] string? code,
            CancellationToken cancellationToken
        )
        {
            var result = await auth.ConfirmEmailAsync(userId ?? string.Empty, code ?? string.Empty, cancellationToken);

            if (!PrefersHtml())
                return result.IsSuccess ? NoContent() : ResultExtensions.Problem(result.Error!, this);

            return Redirect(urls.Value.FrontendUrl($"/email-confirmation?status={ConfirmationStatus(result.Error)}"));
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken
        ) => (await auth.LogoutAsync(request, cancellationToken)).ToNoContentResult(this);

        private bool PrefersHtml() =>
            Request
                .GetTypedHeaders()
                .Accept.Any(media => media.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase));

        private static string ConfirmationStatus(Error? error) =>
            error?.Code switch
            {
                null => "success",
                "auth.user_not_found" => "not-found",
                "auth.confirmation_failed" => "failed",
                _ => "invalid",
            };
    }
}
