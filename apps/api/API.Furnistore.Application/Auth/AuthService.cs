using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Furnistore.Application.Common;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace API.Furnistore.Application.Auth
{
    public sealed class JwtOptions
    {
        public required string Secret { get; init; }
        public required string Issuer { get; init; }
        public required string Audience { get; init; }
        public required TimeSpan ExpiryTime { get; init; }
        public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(30);
    }

    public sealed class AuthService(
        UserManager<IdentityUser> userManager,
        APIFurnistoreContext db,
        JwtOptions jwt,
        TokenValidationParameters tokenValidationParameters,
        IVerificationEmailSender emailSender,
        IEmailConfirmationLinkBuilder linkBuilder,
        ILogger<AuthService> logger
    )
    {
        public async Task<Result<RegisterResponse>> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken
        )
        {
            var email = request.EmailAddress.Trim();

            if (await userManager.FindByEmailAsync(email) is not null)
            {
                logger.LogWarning(
                    ApiEvents.RegistrationRejected,
                    "Registration rejected for {EmailMasked}: email already exists",
                    Mask(email)
                );
                return Result.Fail<RegisterResponse>(
                    Error.Conflict("auth.email_exists", "Ya existe una cuenta con ese correo.")
                );
            }

            var user = new IdentityUser
            {
                Email = email,
                UserName = email,
                EmailConfirmed = false,
            };

            var created = await userManager.CreateAsync(user, request.Password);

            if (!created.Succeeded)
            {
                var reasons = string.Join("; ", created.Errors.Select(e => e.Description));
                logger.LogWarning(
                    ApiEvents.RegistrationRejected,
                    "Registration rejected for {EmailMasked}: {Reasons}",
                    Mask(email),
                    reasons
                );
                return Result.Fail<RegisterResponse>(
                    Error.Validation("auth.registration_failed", reasons)
                );
            }

            logger.LogInformation(ApiEvents.UserRegistered, "User {UserId} registered", user.Id);

            var emailSent = await TrySendVerificationEmailAsync(user, cancellationToken);

            return Result.Ok(new RegisterResponse(emailSent));
        }

        public async Task<Result<AuthTokensResponse>> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken
        )
        {
            var email = request.Email.Trim();
            var user = await userManager.FindByEmailAsync(email);

            if (user is null)
                return LoginFailure(email, "user_not_found");

            if (!user.EmailConfirmed)
            {
                logger.LogWarning(
                    ApiEvents.LoginFailed,
                    "Login failed for {EmailMasked}: {Reason}",
                    Mask(email),
                    "email_not_confirmed"
                );
                return Result.Fail<AuthTokensResponse>(
                    Error.Unauthorized(
                        "auth.email_not_confirmed",
                        "Necesitas confirmar tu correo antes de iniciar sesión."
                    )
                );
            }

            if (!await userManager.CheckPasswordAsync(user, request.Password))
                return LoginFailure(email, "bad_password");

            var tokens = await IssueTokensAsync(user, cancellationToken);

            logger.LogInformation(ApiEvents.LoginSucceeded, "User {UserId} logged in", user.Id);

            return Result.Ok(tokens);
        }

        public async Task<Result<AuthTokensResponse>> RefreshAsync(
            RefreshTokenRequest request,
            CancellationToken cancellationToken
        )
        {
            var handler = new JwtSecurityTokenHandler();

            // El JWT que se renueva ya está vencido por definición: se valida todo menos la vigencia.
            var parameters = tokenValidationParameters.Clone();
            parameters.ValidateLifetime = false;

            ClaimsPrincipal principal;
            SecurityToken validatedToken;

            try
            {
                principal = handler.ValidateToken(request.Token, parameters, out validatedToken);
            }
            catch (SecurityTokenException)
            {
                return RefreshFailure("malformed_jwt", null);
            }

            if (
                validatedToken is not JwtSecurityToken jwtToken
                || !jwtToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
                return RefreshFailure("unexpected_algorithm", null);

            var storedToken = await db.RefreshTokens.FirstOrDefaultAsync(
                t => t.Token == request.RefreshToken,
                cancellationToken
            );

            if (storedToken is null)
                return RefreshFailure("refresh_token_unknown", null);

            if (storedToken.IsUsed || storedToken.IsRevoked)
                return RefreshFailure("refresh_token_spent", storedToken.UserId);

            if (storedToken.ExpiryDate < DateTime.UtcNow)
            {
                logger.LogWarning(
                    ApiEvents.RefreshTokenRejected,
                    "Refresh rejected for {UserId}: {Reason}",
                    storedToken.UserId,
                    "refresh_token_expired"
                );
                return Result.Fail<AuthTokensResponse>(
                    Error.Unauthorized("auth.refresh_token_expired", "La sesión expiró. Inicia sesión de nuevo.")
                );
            }

            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (jti is null || jti != storedToken.JwtId)
                return RefreshFailure("jti_mismatch", storedToken.UserId);

            var user = await userManager.FindByIdAsync(storedToken.UserId);

            if (user is null)
                return RefreshFailure("user_gone", storedToken.UserId);

            storedToken.IsUsed = true;
            await db.SaveChangesAsync(cancellationToken);

            var tokens = await IssueTokensAsync(user, cancellationToken);

            logger.LogInformation(
                ApiEvents.TokenRefreshed,
                "Token refreshed for user {UserId}",
                user.Id
            );

            return Result.Ok(tokens);
        }

        public async Task<Result> ConfirmEmailAsync(
            string userId,
            string code,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
                return Result.Fail(
                    Error.Validation("auth.invalid_confirmation_url", "El enlace de confirmación no es válido.")
                );

            var user = await userManager.FindByIdAsync(userId);

            if (user is null)
                return Result.Fail(
                    Error.NotFound("auth.user_not_found", "El enlace de confirmación no es válido.")
                );

            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(code));
            }
            catch (FormatException)
            {
                return Result.Fail(
                    Error.Validation("auth.invalid_confirmation_code", "El enlace de confirmación no es válido.")
                );
            }

            var result = await userManager.ConfirmEmailAsync(user, decoded);

            if (!result.Succeeded)
            {
                logger.LogWarning(
                    ApiEvents.EmailConfirmationFailed,
                    "Email confirmation failed for {UserId}",
                    userId
                );
                return Result.Fail(
                    Error.Validation("auth.confirmation_failed", "No se pudo confirmar el correo.")
                );
            }

            logger.LogInformation(ApiEvents.EmailConfirmed, "Email confirmed for {UserId}", userId);

            return Result.Ok();
        }

        private async Task<AuthTokensResponse> IssueTokensAsync(
            IdentityUser user,
            CancellationToken cancellationToken
        )
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwt.Secret);
            var jti = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow;

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                    new[]
                    {
                        new Claim("Id", user.Id),
                        new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
                        new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                        new Claim(JwtRegisteredClaimNames.Jti, jti),
                    }
                ),
                IssuedAt = now,
                NotBefore = now,
                Expires = now.Add(jwt.ExpiryTime),
                Issuer = jwt.Issuer,
                Audience = jwt.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256
                ),
            };

            var jwtToken = handler.WriteToken(handler.CreateToken(descriptor));

            var refreshToken = new RefreshToken
            {
                JwtId = jti,
                Token = RandomGenerator.GenerateRandomString(48),
                AddedDate = now,
                ExpiryDate = now.Add(jwt.RefreshTokenLifetime),
                IsRevoked = false,
                IsUsed = false,
                UserId = user.Id,
            };

            db.RefreshTokens.Add(refreshToken);
            await db.SaveChangesAsync(cancellationToken);

            return new AuthTokensResponse(jwtToken, refreshToken.Token);
        }

        private async Task<bool> TrySendVerificationEmailAsync(
            IdentityUser user,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var rawCode = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var code = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(rawCode));
                var link = linkBuilder.Build(user.Id, code);

                await emailSender.SendAsync(
                    user.Email!,
                    "Confirma tu correo",
                    $"Confirma tu cuenta <a href=\"{link}\">haciendo clic aquí</a>.",
                    cancellationToken
                );

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ApiEvents.EmailSendFailed,
                    ex,
                    "Verification email could not be sent for {UserId}",
                    user.Id
                );
                return false;
            }
        }

        private Result<AuthTokensResponse> LoginFailure(string email, string reason)
        {
            logger.LogWarning(
                ApiEvents.LoginFailed,
                "Login failed for {EmailMasked}: {Reason}",
                Mask(email),
                reason
            );

            return Result.Fail<AuthTokensResponse>(
                Error.Unauthorized("auth.invalid_credentials", "Correo o contraseña incorrectos.")
            );
        }

        private Result<AuthTokensResponse> RefreshFailure(string reason, string? userId)
        {
            logger.LogWarning(
                ApiEvents.RefreshTokenRejected,
                "Refresh rejected for {UserId}: {Reason}",
                userId ?? "unknown",
                reason
            );

            return Result.Fail<AuthTokensResponse>(
                Error.Unauthorized("auth.invalid_refresh_token", "La sesión no es válida. Inicia sesión de nuevo.")
            );
        }

        private static string Mask(string email)
        {
            var at = email.IndexOf('@');
            return at <= 1 ? "***" : $"{email[0]}***{email[at..]}";
        }
    }
}
