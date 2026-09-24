using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Furnistore.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    // Lo de abajo es para limitar el numero de intentos por minuto de un usaurio...
    [EnableRateLimiting(AuthRateLimits.Session)]
    [Route("api/authentication")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public sealed class AuthenticationController(AuthService auth) : ControllerBase
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

        [HttpPost("verify-email")]
        [EnableRateLimiting(AuthRateLimits.Credentials)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail(
            VerifyEmailRequest request,
            CancellationToken cancellationToken
        ) => (await auth.VerifyEmailAsync(request, cancellationToken)).ToNoContentResult(this);

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken
        ) => (await auth.LogoutAsync(request, cancellationToken)).ToNoContentResult(this);
    }
}
