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
        public TimeSpan RefreshTokenReuseGrace { get; init; } = TimeSpan.FromSeconds(30);
    }

    public sealed class AuthService(
        UserManager<IdentityUser> userManager,
        APIFurnistoreContext db,
        JwtOptions jwt,
        TokenValidationParameters tokenValidationParameters,
        IVerificationEmailSender emailSender,
        TimeProvider clock,
        ILogger<AuthService> logger
    )
    {
        public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

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

            db.Clients.Add(new Client
            {
                UserId = user.Id,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
            });
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(ApiEvents.UserRegistered, "User {UserId} registered", user.Id);
            
            // Se asinga un rol user por defecto.
            if (!await userManager.IsInRoleAsync(user, "User"))
                await userManager.AddToRoleAsync(user, "User");
            
            var emailSent = await TrySendVerificationCodeAsync(user, request.FirstName.Trim(), cancellationToken);

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

            var disabled = AccountLock.IsDisabled(user);

            if (!disabled && await userManager.IsLockedOutAsync(user))
                return LockedOut(user);

            if (!await userManager.CheckPasswordAsync(user, request.Password))
            {
                if (disabled)
                    return LoginFailure(email, "bad_password");

                await userManager.AccessFailedAsync(user);

                return await userManager.IsLockedOutAsync(user)
                    ? LockedOut(user)
                    : LoginFailure(email, "bad_password");
            }

            if (disabled)
                return Disabled(user);

            if (user.AccessFailedCount > 0)
                await userManager.ResetAccessFailedCountAsync(user);

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

            var tokenHash = RefreshTokenHasher.Hash(request.RefreshToken);
            var storedToken = await db.RefreshTokens.FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash,
                cancellationToken
            );

            if (storedToken is null)
                return RefreshFailure("refresh_token_unknown", null);

            var now = clock.GetUtcNow().UtcDateTime;

            if (storedToken.IsRevoked)
                return RefreshFailure("refresh_token_revoked", storedToken.UserId);

            if (storedToken.IsUsed && !(storedToken.UsedAt > now - jwt.RefreshTokenReuseGrace))
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

            if (AccountLock.IsDisabled(user))
                return Disabled(user);

            if (!storedToken.IsUsed)
            {
                storedToken.IsUsed = true;
                storedToken.UsedAt = now;
                await db.SaveChangesAsync(cancellationToken);
            }

            var tokens = await IssueTokensAsync(user, cancellationToken);

            logger.LogInformation(
                ApiEvents.TokenRefreshed,
                "Token refreshed for user {UserId}",
                user.Id
            );

            return Result.Ok(tokens);
        }

        public async Task<Result> ResendConfirmationAsync(
            ResendConfirmationRequest request,
            CancellationToken cancellationToken
        )
        {
            var email = request.Email.Trim();
            var user = await userManager.FindByEmailAsync(email);

            if (user is null || user.EmailConfirmed)
            {
                logger.LogInformation(
                    ApiEvents.ConfirmationEmailSkipped,
                    "Verification code resend skipped for {EmailMasked}: {Reason}",
                    Mask(email),
                    user is null ? "unknown_email" : "already_confirmed"
                );
                return Result.Ok();
            }

            var now = clock.GetUtcNow().UtcDateTime;
            var coolingDown = await db.EmailVerificationCodes.AnyAsync(
                c => c.UserId == user.Id && c.CreatedAt > now - ResendCooldown,
                cancellationToken
            );

            if (coolingDown)
            {
                logger.LogInformation(
                    ApiEvents.ConfirmationEmailSkipped,
                    "Verification code resend skipped for {UserId}: {Reason}",
                    user.Id,
                    "cooldown"
                );
                return Result.Ok();
            }

            var firstName = await db
                .Clients.Where(c => c.UserId == user.Id)
                .Select(c => c.FirstName)
                .FirstOrDefaultAsync(cancellationToken);

            await TrySendVerificationCodeAsync(user, firstName ?? string.Empty, cancellationToken);

            return Result.Ok();
        }

        public async Task<Result> VerifyEmailAsync(
            VerifyEmailRequest request,
            CancellationToken cancellationToken
        )
        {
            var email = request.Email.Trim();
            var user = await userManager.FindByEmailAsync(email);

            var stored = user is null
                ? null
                : await db.EmailVerificationCodes.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);

            if (user is null || stored is null || stored.ExpiresAt <= clock.GetUtcNow().UtcDateTime)
                return VerificationFailure(user?.Id, "code_expired", Error.Validation(
                    "auth.code_expired",
                    "El código venció o no existe. Pide uno nuevo."
                ));

            var attemptReserved = await db
                .EmailVerificationCodes.Where(c => c.Id == stored.Id && c.FailedAttempts < VerificationCodes.MaxAttempts)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(c => c.FailedAttempts, c => c.FailedAttempts + 1),
                    cancellationToken
                );

            if (attemptReserved == 0)
                return await TooManyCodeAttemptsAsync(user, stored.Id, cancellationToken);

            var submitted = VerificationCodes.Hash(jwt.Secret, user.Id, request.Code);

            if (!VerificationCodes.Matches(stored.CodeHash, submitted))
            {
                if (stored.FailedAttempts + 1 >= VerificationCodes.MaxAttempts)
                    return await TooManyCodeAttemptsAsync(user, stored.Id, cancellationToken);

                return VerificationFailure(user.Id, "code_invalid", Error.Validation(
                    "auth.code_invalid",
                    "Código incorrecto. Revisa el correo e intenta de nuevo."
                ));
            }

            await db.EmailVerificationCodes.Where(c => c.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                var updated = await userManager.UpdateAsync(user);

                if (!updated.Succeeded)
                {
                    logger.LogError(
                        ApiEvents.EmailConfirmationFailed,
                        "Email confirmation could not be saved for {UserId}: {Errors}",
                        user.Id,
                        string.Join("; ", updated.Errors.Select(e => e.Code))
                    );
                    return Result.Fail(Error.Unexpected("auth.confirmation_failed", "No se pudo confirmar el correo."));
                }
            }

            logger.LogInformation(ApiEvents.EmailConfirmed, "Email confirmed for {UserId}", user.Id);

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
            // cojo el rol del usuario
            var roles = await userManager.GetRolesAsync(user);

            // Aqui cojo el clain 
            var claims = new List<Claim>
            {
                new Claim("Id", user.Id),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
            };

            // Por cada rol que tenga el usaurio le pongo un Clain
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
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

            var rawRefreshToken = RandomGenerator.GenerateRandomString(48);
            var refreshToken = new RefreshToken
            {
                JwtId = jti,
                TokenHash = RefreshTokenHasher.Hash(rawRefreshToken),
                AddedDate = now,
                ExpiryDate = now.Add(jwt.RefreshTokenLifetime),
                IsRevoked = false,
                IsUsed = false,
                UserId = user.Id,
            };

            db.RefreshTokens.Add(refreshToken);
            await db.SaveChangesAsync(cancellationToken);

            return new AuthTokensResponse(jwtToken, rawRefreshToken);
        }

        private async Task<bool> TrySendVerificationCodeAsync(
            IdentityUser user,
            string firstName,
            CancellationToken cancellationToken
        )
        {
            var code = VerificationCodes.Generate();
            var now = clock.GetUtcNow().UtcDateTime;

            try
            {
                await db.EmailVerificationCodes.Where(c => c.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
                db.EmailVerificationCodes.Add(
                    new EmailVerificationCode
                    {
                        UserId = user.Id,
                        CodeHash = VerificationCodes.Hash(jwt.Secret, user.Id, code),
                        CreatedAt = now,
                        ExpiresAt = now + VerificationCodes.Lifetime,
                    }
                );
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                logger.LogInformation(
                    ApiEvents.ConfirmationEmailSkipped,
                    "Verification code skipped for {UserId}: {Reason}",
                    user.Id,
                    "concurrent_request"
                );
                return false;
            }

            try
            {
                await emailSender.SendAsync(
                    AuthEmails.VerificationCode(user.Email!, firstName, code, VerificationCodes.Lifetime),
                    cancellationToken
                );

                logger.LogInformation(
                    ApiEvents.ConfirmationEmailSent,
                    "Verification code sent for {UserId}",
                    user.Id
                );

                return true;
            }
            catch (Exception ex)
            {
                await db.EmailVerificationCodes.Where(c => c.UserId == user.Id).ExecuteDeleteAsync(CancellationToken.None);
                logger.LogError(
                    ApiEvents.EmailSendFailed,
                    ex,
                    "Verification code email could not be sent for {UserId}",
                    user.Id
                );
                return false;
            }
        }

        private async Task<Result> TooManyCodeAttemptsAsync(IdentityUser user, int codeId, CancellationToken cancellationToken)
        {
            await db.EmailVerificationCodes.Where(c => c.Id == codeId).ExecuteDeleteAsync(cancellationToken);

            return VerificationFailure(user.Id, "attempts_exceeded", Error.TooManyRequests(
                "auth.code_attempts_exceeded",
                "Demasiados intentos. Pide un código nuevo."
            ));
        }

        private Result VerificationFailure(string? userId, string reason, Error error)
        {
            logger.LogWarning(
                ApiEvents.EmailConfirmationFailed,
                "Email verification rejected for {UserId}: {Reason}",
                userId ?? "unknown",
                reason
            );
            return Result.Fail(error);
        }

        private Result<AuthTokensResponse> Disabled(IdentityUser user)
        {
            logger.LogWarning(ApiEvents.LoginLockedOut, "Session rejected for {UserId}: account disabled", user.Id);

            return Result.Fail<AuthTokensResponse>(
                Error.Forbidden(
                    "auth.account_disabled",
                    "Esta cuenta está desactivada. Escríbenos si crees que es un error."
                )
            );
        }

        private Result<AuthTokensResponse> LockedOut(IdentityUser user)
        {
            logger.LogWarning(ApiEvents.LoginLockedOut, "Login rejected for {UserId}: locked out", user.Id);

            return Result.Fail<AuthTokensResponse>(
                Error.TooManyRequests(
                    "auth.locked_out",
                    "Demasiados intentos fallidos. Espera unos minutos e intenta de nuevo."
                )
            );
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

        public async Task<Result> LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken
        )
        {
            var tokenHash = RefreshTokenHasher.Hash(request.RefreshToken);
            var storedToken = await db.RefreshTokens.FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash,
                cancellationToken
            );

            if (storedToken is null)
                return Result.Ok();

            if (storedToken.IsRevoked)
                return Result.Ok();

            storedToken.IsRevoked = true;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.LogoutSucceeded,   
                "Refresh token revoked for user {UserId}",
                storedToken.UserId
            );

            return Result.Ok();
        }
    }
}
