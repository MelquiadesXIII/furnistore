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
    [EnableRateLimiting("auth")]
    [Route("api/authentication")]
    public sealed class AuthenticationController(AuthService auth) : ControllerBase
    {
        [HttpPost("register")]
        [ProducesResponseType<RegisterResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(
            RegisterRequest request,
            CancellationToken cancellationToken
        ) => (await auth.RegisterAsync(request, cancellationToken)).ToActionResult(this);

        [HttpPost("login")]
        [ProducesResponseType<AuthTokensResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            LoginRequest request,
            CancellationToken cancellationToken
        ) => (await auth.LoginAsync(request, cancellationToken)).ToActionResult(this);

        [HttpPost("refresh-token")]
        [ProducesResponseType<AuthTokensResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken(
            RefreshTokenRequest request,
            CancellationToken cancellationToken
        ) => (await auth.RefreshAsync(request, cancellationToken)).ToActionResult(this);

        [HttpGet("confirm-email", Name = "ConfirmEmail")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmEmail(
            [FromQuery] string userId,
            [FromQuery] string code,
            CancellationToken cancellationToken
        ) =>
            (await auth.ConfirmEmailAsync(userId, code, cancellationToken)).ToEmailConfirmationResult(this);
    }
}
