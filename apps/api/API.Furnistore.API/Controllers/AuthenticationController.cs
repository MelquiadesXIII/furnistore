using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
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
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmEmail(
            [FromQuery] string userId,
            [FromQuery] string code,
            CancellationToken cancellationToken
        ) =>
            (await auth.ConfirmEmailAsync(userId, code, cancellationToken)).ToNoContentResult(this);
    }
}
