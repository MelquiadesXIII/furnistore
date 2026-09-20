using System.Text;
using API.Furnistore.Application.Auth;
using API.Furnistore.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace API.Furnistore.IntegrationTests
{
    public sealed class AuthFlowTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
    {
        private const string Secret = "integration-tests-secret-with-at-least-32-bytes!";
        private const string Password = "Password123";

        [Fact]
        public async Task Register_creates_a_client_without_invented_data()
        {
            var email = NewEmail();
            var outbox = new Outbox();

            var result = await WithAuthAsync(outbox, auth => auth.RegisterAsync(Register(email), CancellationToken.None));

            Assert.True(result.IsSuccess, result.Error?.Message);
            Assert.True(result.Value.EmailSent);

            await using var db = fixture.CreateContext();
            var client = await db.Clients.SingleAsync(c => db.Users.Any(u => u.Id == c.UserId && u.Email == email));
            Assert.Equal("Lucía", client.FirstName);
            Assert.Equal("Gómez Pérez", client.LastName);
            Assert.Null(client.Phone);
            Assert.Null(client.Street);
            Assert.False(client.IsProfileComplete);
        }

        [Fact]
        public async Task Login_requires_a_confirmed_email_and_refresh_tokens_are_stored_hashed()
        {
            var email = NewEmail();
            var outbox = new Outbox();
            await WithAuthAsync(outbox, auth => auth.RegisterAsync(Register(email), CancellationToken.None));

            var beforeConfirming = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email), CancellationToken.None));
            Assert.Equal("auth.email_not_confirmed", beforeConfirming.Error!.Code);

            var confirmed = await WithAuthAsync(
                outbox,
                auth => auth.ConfirmEmailAsync(outbox.UserId!, outbox.Code!, CancellationToken.None)
            );
            Assert.True(confirmed.IsSuccess, confirmed.Error?.Message);

            var login = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email), CancellationToken.None));
            Assert.True(login.IsSuccess, login.Error?.Message);
            var tokens = login.Value;

            await using (var db = fixture.CreateContext())
            {
                var stored = await db.RefreshTokens.SingleAsync(t => t.UserId == outbox.UserId);
                Assert.Equal(RefreshTokenHasher.Hash(tokens.RefreshToken), stored.TokenHash);
                Assert.NotEqual(tokens.RefreshToken, stored.TokenHash);
            }

            var refreshed = await WithAuthAsync(
                outbox,
                auth => auth.RefreshAsync(
                    new RefreshTokenRequest { Token = tokens.Token, RefreshToken = tokens.RefreshToken },
                    CancellationToken.None
                )
            );
            Assert.True(refreshed.IsSuccess, refreshed.Error?.Message);

            var reused = await WithAuthAsync(
                outbox,
                auth => auth.RefreshAsync(
                    new RefreshTokenRequest { Token = tokens.Token, RefreshToken = tokens.RefreshToken },
                    CancellationToken.None
                )
            );
            Assert.Equal("auth.invalid_refresh_token", reused.Error!.Code);

            await WithAuthAsync(
                outbox,
                auth => auth.LogoutAsync(new LogoutRequest { RefreshToken = refreshed.Value.RefreshToken }, CancellationToken.None)
            );

            await using (var db = fixture.CreateContext())
            {
                var latest = await db.RefreshTokens.SingleAsync(
                    t => t.TokenHash == RefreshTokenHasher.Hash(refreshed.Value.RefreshToken)
                );
                Assert.True(latest.IsRevoked);
            }
        }

        private async Task<T> WithAuthAsync<T>(Outbox outbox, Func<AuthService, Task<T>> action)
        {
            await using var scope = fixture.CreateScope();
            var provider = scope.ServiceProvider;

            var roles = provider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roles.RoleExistsAsync("User"))
                await roles.CreateAsync(new IdentityRole("User"));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
            var auth = new AuthService(
                provider.GetRequiredService<UserManager<IdentityUser>>(),
                provider.GetRequiredService<APIFurnistoreContext>(),
                new JwtOptions
                {
                    Secret = Secret,
                    Issuer = "tests",
                    Audience = "tests",
                    ExpiryTime = TimeSpan.FromMinutes(5),
                },
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidIssuer = "tests",
                    ValidAudience = "tests",
                    ClockSkew = TimeSpan.Zero,
                },
                outbox,
                outbox,
                NullLogger<AuthService>.Instance
            );

            return await action(auth);
        }

        private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

        private static RegisterRequest Register(string email) =>
            new()
            {
                FirstName = "Lucía",
                LastName = "Gómez Pérez",
                EmailAddress = email,
                Password = Password,
            };

        private static LoginRequest Login(string email) => new() { Email = email, Password = Password };

        private sealed class Outbox : IVerificationEmailSender, IEmailConfirmationLinkBuilder
        {
            public string? UserId { get; private set; }

            public string? Code { get; private set; }

            public string Build(string userId, string code)
            {
                UserId = userId;
                Code = code;
                return $"https://tests.local/confirm?userId={userId}&code={code}";
            }

            public Task SendAsync(string email, string subject, string htmlBody, CancellationToken cancellationToken) =>
                Task.CompletedTask;
        }
    }
}
