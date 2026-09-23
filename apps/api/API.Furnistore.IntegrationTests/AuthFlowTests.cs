using System.Text;
using API.Furnistore.Application.Auth;
using API.Furnistore.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace API.Furnistore.IntegrationTests
{
    public sealed class AuthFlowTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
    {
        private const string Secret = "integration-tests-secret-with-at-least-32-bytes!";
        private const string Password = "Password123";

        private readonly MemoryCache cache = new(new MemoryCacheOptions());

        [Fact]
        public async Task Register_creates_a_client_without_invented_data_and_sends_a_spanish_email()
        {
            var email = NewEmail();
            var outbox = new Outbox();

            var result = await WithAuthAsync(outbox, auth => auth.RegisterAsync(Register(email, firstName: "<b>Lucía</b>"), CancellationToken.None));

            Assert.True(result.IsSuccess, result.Error?.Message);
            Assert.True(result.Value.EmailSent);

            await using var db = fixture.CreateContext();
            var client = await db.Clients.SingleAsync(c => db.Users.Any(u => u.Id == c.UserId && u.Email == email));
            Assert.Equal("<b>Lucía</b>", client.FirstName);
            Assert.Equal("Gómez Pérez", client.LastName);
            Assert.Null(client.Phone);
            Assert.Null(client.Street);
            Assert.False(client.IsProfileComplete);

            var message = Assert.Single(outbox.Sent);
            Assert.Equal(email, message.To);
            Assert.Equal("Confirma tu correo en Furnistore", message.Subject);
            Assert.Contains(outbox.Link!, message.TextBody);
            Assert.Contains("Hola &lt;b&gt;Lucía&lt;/b&gt;:", message.HtmlBody);
            Assert.DoesNotContain("<b>Lucía</b>", message.HtmlBody);
        }

        [Fact]
        public async Task Login_only_reveals_an_unconfirmed_email_to_someone_who_knows_the_password()
        {
            var email = NewEmail();
            var outbox = new Outbox();
            await WithAuthAsync(outbox, auth => auth.RegisterAsync(Register(email), CancellationToken.None));

            var wrongPassword = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email, "Wrong12345"), CancellationToken.None));
            Assert.Equal("auth.invalid_credentials", wrongPassword.Error!.Code);

            var rightPassword = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email), CancellationToken.None));
            Assert.Equal("auth.email_not_confirmed", rightPassword.Error!.Code);
        }

        [Fact]
        public async Task Confirming_the_email_unlocks_login_and_refresh_tokens_are_stored_hashed()
        {
            var email = NewEmail();
            var outbox = new Outbox();
            await WithAuthAsync(outbox, auth => auth.RegisterAsync(Register(email), CancellationToken.None));

            var confirmed = await WithAuthAsync(
                outbox,
                auth => auth.ConfirmEmailAsync(outbox.UserId!, outbox.Code!, CancellationToken.None)
            );
            Assert.True(confirmed.IsSuccess, confirmed.Error?.Message);

            var confirmedAgain = await WithAuthAsync(
                outbox,
                auth => auth.ConfirmEmailAsync(outbox.UserId!, outbox.Code!, CancellationToken.None)
            );
            Assert.True(confirmedAgain.IsSuccess, confirmedAgain.Error?.Message);

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

        [Fact]
        public async Task Five_wrong_passwords_lock_the_account_even_for_the_right_password()
        {
            var email = await RegisterConfirmedAsync();
            var outbox = new Outbox();

            for (var attempt = 1; attempt <= 4; attempt++)
            {
                var failed = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email, "Wrong12345"), CancellationToken.None));
                Assert.Equal("auth.invalid_credentials", failed.Error!.Code);
            }

            var fifth = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email, "Wrong12345"), CancellationToken.None));
            Assert.Equal("auth.locked_out", fifth.Error!.Code);

            var rightPassword = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email), CancellationToken.None));
            Assert.Equal("auth.locked_out", rightPassword.Error!.Code);
        }

        [Fact]
        public async Task A_successful_login_resets_the_failed_attempts()
        {
            var email = await RegisterConfirmedAsync();
            var outbox = new Outbox();

            for (var attempt = 1; attempt <= 4; attempt++)
                await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email, "Wrong12345"), CancellationToken.None));

            var success = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email), CancellationToken.None));
            Assert.True(success.IsSuccess, success.Error?.Message);

            var afterReset = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email, "Wrong12345"), CancellationToken.None));
            Assert.Equal("auth.invalid_credentials", afterReset.Error!.Code);
        }

        [Fact]
        public async Task Resending_the_confirmation_never_reveals_accounts_and_respects_the_cooldown()
        {
            var registrationOutbox = new Outbox();
            var email = NewEmail();
            await WithAuthAsync(registrationOutbox, auth => auth.RegisterAsync(Register(email), CancellationToken.None));

            var outbox = new Outbox();

            var unknown = await WithAuthAsync(outbox, auth => auth.ResendConfirmationAsync(Resend(NewEmail()), CancellationToken.None));
            Assert.True(unknown.IsSuccess);
            Assert.Empty(outbox.Sent);

            var withinCooldown = await WithAuthAsync(outbox, auth => auth.ResendConfirmationAsync(Resend(email), CancellationToken.None));
            Assert.True(withinCooldown.IsSuccess);
            Assert.Empty(outbox.Sent);

            cache.Compact(1.0);

            var resent = await WithAuthAsync(outbox, auth => auth.ResendConfirmationAsync(Resend(email), CancellationToken.None));
            Assert.True(resent.IsSuccess);
            var message = Assert.Single(outbox.Sent);
            Assert.Contains("Hola Lucía:", message.TextBody);

            var confirmedEmail = await RegisterConfirmedAsync();
            cache.Compact(1.0);
            var alreadyConfirmed = await WithAuthAsync(outbox, auth => auth.ResendConfirmationAsync(Resend(confirmedEmail), CancellationToken.None));
            Assert.True(alreadyConfirmed.IsSuccess);
            Assert.Single(outbox.Sent);
        }

        private async Task<string> RegisterConfirmedAsync()
        {
            var email = NewEmail();
            var outbox = new Outbox();
            await WithAuthAsync(outbox, auth => auth.RegisterAsync(Register(email), CancellationToken.None));
            var confirmed = await WithAuthAsync(
                outbox,
                auth => auth.ConfirmEmailAsync(outbox.UserId!, outbox.Code!, CancellationToken.None)
            );
            Assert.True(confirmed.IsSuccess, confirmed.Error?.Message);
            return email;
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
                cache,
                NullLogger<AuthService>.Instance
            );

            return await action(auth);
        }

        private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

        private static RegisterRequest Register(string email, string firstName = "Lucía") =>
            new()
            {
                FirstName = firstName,
                LastName = "Gómez Pérez",
                EmailAddress = email,
                Password = Password,
            };

        private static LoginRequest Login(string email, string password = Password) =>
            new() { Email = email, Password = password };

        private static ResendConfirmationRequest Resend(string email) => new() { Email = email };

        private sealed class Outbox : IVerificationEmailSender, IEmailConfirmationLinkBuilder
        {
            public List<EmailMessage> Sent { get; } = [];

            public string? UserId { get; private set; }

            public string? Code { get; private set; }

            public string? Link { get; private set; }

            public string Build(string userId, string code)
            {
                UserId = userId;
                Code = code;
                Link = $"https://tests.local/confirm?userId={userId}&code={code}";
                return Link;
            }

            public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
            {
                Sent.Add(message);
                return Task.CompletedTask;
            }
        }
    }
}
