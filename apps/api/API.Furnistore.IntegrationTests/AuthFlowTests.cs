using System.Text;
using System.Text.RegularExpressions;
using API.Furnistore.Application.Admin.Customers;
using API.Furnistore.Application.Auth;
using API.Furnistore.Data;
using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace API.Furnistore.IntegrationTests
{
    public sealed partial class AuthFlowTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
    {
        private const string Secret = "integration-tests-secret-with-at-least-32-bytes!";
        private const string Password = "Password123";

        private readonly TestClock clock = new();

        [Fact]
        public async Task Register_creates_a_client_without_invented_data_and_emails_a_code()
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
            Assert.False(client.IsProfileComplete);

            var message = Assert.Single(outbox.Sent);
            Assert.Equal(email, message.To);
            Assert.Equal($"Tu código de Furnistore: {outbox.LastCode}", message.Subject);
            Assert.Matches(@"^\d{6}$", outbox.LastCode);
            Assert.Contains($"Tu código para confirmar tu correo en Furnistore es: {outbox.LastCode}", message.TextBody);
            Assert.Contains("Hola &lt;b&gt;Lucía&lt;/b&gt;:", message.HtmlBody);
            Assert.DoesNotContain("<b>Lucía</b>", message.HtmlBody);

            var stored = await db.EmailVerificationCodes.SingleAsync(c => db.Users.Any(u => u.Id == c.UserId && u.Email == email));
            Assert.DoesNotContain(outbox.LastCode, stored.CodeHash);
            Assert.Equal(64, stored.CodeHash.Length);
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
        public async Task The_code_confirms_the_email_once_and_unlocks_login()
        {
            var email = NewEmail();
            var outbox = new Outbox();
            await WithAuthAsync(outbox, auth => auth.RegisterAsync(Register(email), CancellationToken.None));
            var code = outbox.LastCode;

            var verified = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(email, code), CancellationToken.None));
            Assert.True(verified.IsSuccess, verified.Error?.Message);

            var reused = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(email, code), CancellationToken.None));
            Assert.Equal("auth.code_expired", reused.Error!.Code);

            var login = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email), CancellationToken.None));
            Assert.True(login.IsSuccess, login.Error?.Message);

            await using var db = fixture.CreateContext();
            Assert.False(await db.EmailVerificationCodes.AnyAsync(c => db.Users.Any(u => u.Id == c.UserId && u.Email == email)));
        }

        [Fact]
        public async Task A_wrong_code_is_rejected_and_does_not_confirm()
        {
            var email = NewEmail();
            var outbox = new Outbox();
            await WithAuthAsync(outbox, auth => auth.RegisterAsync(Register(email), CancellationToken.None));
            var wrong = outbox.LastCode == "000000" ? "111111" : "000000";

            var rejected = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(email, wrong), CancellationToken.None));
            Assert.Equal("auth.code_invalid", rejected.Error!.Code);

            var login = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email), CancellationToken.None));
            Assert.Equal("auth.email_not_confirmed", login.Error!.Code);

            var stillValid = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(email, outbox.LastCode), CancellationToken.None));
            Assert.True(stillValid.IsSuccess, stillValid.Error?.Message);
        }

        [Fact]
        public async Task Five_wrong_codes_burn_the_code_even_for_the_right_one()
        {
            var email = NewEmail();
            var outbox = new Outbox();
            await WithAuthAsync(outbox, auth => auth.RegisterAsync(Register(email), CancellationToken.None));
            var wrong = outbox.LastCode == "000000" ? "111111" : "000000";

            for (var attempt = 1; attempt <= 4; attempt++)
            {
                var failed = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(email, wrong), CancellationToken.None));
                Assert.Equal("auth.code_invalid", failed.Error!.Code);
            }

            var fifth = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(email, wrong), CancellationToken.None));
            Assert.Equal("auth.code_attempts_exceeded", fifth.Error!.Code);

            var rightCode = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(email, outbox.LastCode), CancellationToken.None));
            Assert.Equal("auth.code_expired", rightCode.Error!.Code);

            var login = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email), CancellationToken.None));
            Assert.Equal("auth.email_not_confirmed", login.Error!.Code);
        }

        [Fact]
        public async Task Parallel_guesses_never_get_more_than_five_attempts()
        {
            var email = NewEmail();
            var outbox = new Outbox();
            await WithAuthAsync(outbox, auth => auth.RegisterAsync(Register(email), CancellationToken.None));
            var wrong = outbox.LastCode == "000000" ? "111111" : "000000";

            var results = await Task.WhenAll(
                Enumerable.Range(0, 12).Select(_ =>
                    WithAuthAsync(new Outbox(), auth => auth.VerifyEmailAsync(Verify(email, wrong), CancellationToken.None))
                )
            );

            Assert.True(results.Count(r => r.Error!.Code == "auth.code_invalid") <= 5);
            Assert.All(results, r => Assert.False(r.IsSuccess));

            var late = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(email, outbox.LastCode), CancellationToken.None));
            Assert.False(late.IsSuccess);
        }

        [Fact]
        public async Task An_expired_code_is_rejected()
        {
            var email = NewEmail();
            var outbox = new Outbox();
            await WithAuthAsync(outbox, auth => auth.RegisterAsync(Register(email), CancellationToken.None));

            clock.Advance(TimeSpan.FromMinutes(16));

            var expired = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(email, outbox.LastCode), CancellationToken.None));
            Assert.Equal("auth.code_expired", expired.Error!.Code);
        }

        [Fact]
        public async Task Verifying_never_reveals_whether_an_account_exists()
        {
            var outbox = new Outbox();

            var unknown = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(NewEmail(), "123456"), CancellationToken.None));
            Assert.Equal("auth.code_expired", unknown.Error!.Code);

            var confirmedEmail = await RegisterConfirmedAsync();
            var confirmed = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(confirmedEmail, "123456"), CancellationToken.None));
            Assert.Equal("auth.code_expired", confirmed.Error!.Code);
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
        public async Task Resending_replaces_the_code_respects_the_cooldown_and_never_reveals_accounts()
        {
            var email = NewEmail();
            var registration = new Outbox();
            await WithAuthAsync(registration, auth => auth.RegisterAsync(Register(email), CancellationToken.None));
            var firstCode = registration.LastCode;

            var outbox = new Outbox();

            var unknown = await WithAuthAsync(outbox, auth => auth.ResendConfirmationAsync(Resend(NewEmail()), CancellationToken.None));
            Assert.True(unknown.IsSuccess);
            Assert.Empty(outbox.Sent);

            var withinCooldown = await WithAuthAsync(outbox, auth => auth.ResendConfirmationAsync(Resend(email), CancellationToken.None));
            Assert.True(withinCooldown.IsSuccess);
            Assert.Empty(outbox.Sent);

            clock.Advance(TimeSpan.FromSeconds(61));

            var resent = await WithAuthAsync(outbox, auth => auth.ResendConfirmationAsync(Resend(email), CancellationToken.None));
            Assert.True(resent.IsSuccess);
            Assert.Single(outbox.Sent);
            Assert.Contains("Hola Lucía:", outbox.Sent[0].TextBody);

            if (outbox.LastCode != firstCode)
            {
                var oldCode = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(email, firstCode), CancellationToken.None));
                Assert.Equal("auth.code_invalid", oldCode.Error!.Code);
            }

            var newCode = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(email, outbox.LastCode), CancellationToken.None));
            Assert.True(newCode.IsSuccess, newCode.Error?.Message);

            var confirmedEmail = await RegisterConfirmedAsync();
            clock.Advance(TimeSpan.FromMinutes(5));
            var before = outbox.Sent.Count;
            var alreadyConfirmed = await WithAuthAsync(outbox, auth => auth.ResendConfirmationAsync(Resend(confirmedEmail), CancellationToken.None));
            Assert.True(alreadyConfirmed.IsSuccess);
            Assert.Equal(before, outbox.Sent.Count);
        }

        [Fact]
        public async Task A_failed_send_does_not_leave_a_code_that_blocks_the_next_attempt()
        {
            var email = NewEmail();
            var outbox = new Outbox { Fail = true };

            var registered = await WithAuthAsync(outbox, auth => auth.RegisterAsync(Register(email), CancellationToken.None));
            Assert.True(registered.IsSuccess);
            Assert.False(registered.Value.EmailSent);

            outbox.Fail = false;
            var resent = await WithAuthAsync(outbox, auth => auth.ResendConfirmationAsync(Resend(email), CancellationToken.None));
            Assert.True(resent.IsSuccess);
            Assert.Single(outbox.Sent);
        }

        [Fact]
        public async Task Refresh_tokens_are_stored_hashed_rotate_and_revoke_on_logout()
        {
            var email = await RegisterConfirmedAsync();
            var outbox = new Outbox();

            var login = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email), CancellationToken.None));
            var tokens = login.Value;

            await using (var db = fixture.CreateContext())
            {
                var stored = await db.RefreshTokens.SingleAsync(t => t.TokenHash == RefreshTokenHasher.Hash(tokens.RefreshToken));
                Assert.NotEqual(tokens.RefreshToken, stored.TokenHash);
            }

            var refreshed = await WithAuthAsync(
                outbox,
                auth => auth.RefreshAsync(new RefreshTokenRequest { Token = tokens.Token, RefreshToken = tokens.RefreshToken }, CancellationToken.None)
            );
            Assert.True(refreshed.IsSuccess, refreshed.Error?.Message);

            var concurrent = await WithAuthAsync(
                outbox,
                auth => auth.RefreshAsync(new RefreshTokenRequest { Token = tokens.Token, RefreshToken = tokens.RefreshToken }, CancellationToken.None)
            );
            Assert.True(concurrent.IsSuccess, "A reuse inside the grace window covers parallel requests");

            clock.Advance(TimeSpan.FromSeconds(31));

            var reused = await WithAuthAsync(
                outbox,
                auth => auth.RefreshAsync(new RefreshTokenRequest { Token = tokens.Token, RefreshToken = tokens.RefreshToken }, CancellationToken.None)
            );
            Assert.Equal("auth.invalid_refresh_token", reused.Error!.Code);

            await WithAuthAsync(
                outbox,
                auth => auth.LogoutAsync(new LogoutRequest { RefreshToken = refreshed.Value.RefreshToken }, CancellationToken.None)
            );

            await using var check = fixture.CreateContext();
            var latest = await check.RefreshTokens.SingleAsync(t => t.TokenHash == RefreshTokenHasher.Hash(refreshed.Value.RefreshToken));
            Assert.True(latest.IsRevoked);
        }

        [Fact]
        public async Task A_disabled_account_cannot_sign_in_or_refresh_and_failed_attempts_do_not_lift_it()
        {
            var email = await RegisterConfirmedAsync();
            var outbox = new Outbox();
            var tokens = (await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email), CancellationToken.None))).Value;

            var customerId = await fixture.RunAsync<AdminCustomerService, int?>(service =>
                service.FindIdByEmailAsync(email, CancellationToken.None)
            );
            var disabled = await fixture.RunAsync<AdminCustomerService, Result<AdminCustomerResponse>>(service =>
                service.DisableAsync(customerId!.Value, "admin-1", CancellationToken.None)
            );
            Assert.True(disabled.IsSuccess, disabled.Error?.Message);
            Assert.True(disabled.Value.Account.IsDisabled);

            for (var attempt = 0; attempt < 6; attempt++)
            {
                var wrong = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email, "Wrong12345"), CancellationToken.None));
                Assert.Equal("auth.invalid_credentials", wrong.Error!.Code);
            }

            clock.Advance(TimeSpan.FromMinutes(30));

            var right = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email), CancellationToken.None));
            Assert.Equal("auth.account_disabled", right.Error!.Code);

            var refresh = await WithAuthAsync(
                outbox,
                auth => auth.RefreshAsync(new RefreshTokenRequest { Token = tokens.Token, RefreshToken = tokens.RefreshToken }, CancellationToken.None)
            );
            Assert.False(refresh.IsSuccess);

            await fixture.RunAsync<AdminCustomerService, Result<AdminCustomerResponse>>(service =>
                service.EnableAsync(customerId!.Value, "admin-1", CancellationToken.None)
            );

            var again = await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email), CancellationToken.None));
            Assert.True(again.IsSuccess, again.Error?.Message);
        }

        [Fact]
        public async Task A_revoked_refresh_token_is_rejected_even_inside_the_grace_window()
        {
            var email = await RegisterConfirmedAsync();
            var outbox = new Outbox();
            var tokens = (await WithAuthAsync(outbox, auth => auth.LoginAsync(Login(email), CancellationToken.None))).Value;
            var request = new RefreshTokenRequest { Token = tokens.Token, RefreshToken = tokens.RefreshToken };

            Assert.True((await WithAuthAsync(outbox, auth => auth.RefreshAsync(request, CancellationToken.None))).IsSuccess);

            await WithAuthAsync(outbox, auth => auth.LogoutAsync(new LogoutRequest { RefreshToken = tokens.RefreshToken }, CancellationToken.None));

            var afterLogout = await WithAuthAsync(outbox, auth => auth.RefreshAsync(request, CancellationToken.None));
            Assert.Equal("auth.invalid_refresh_token", afterLogout.Error!.Code);
        }

        private async Task<string> RegisterConfirmedAsync()
        {
            var email = NewEmail();
            var outbox = new Outbox();
            await WithAuthAsync(outbox, auth => auth.RegisterAsync(Register(email), CancellationToken.None));
            var verified = await WithAuthAsync(outbox, auth => auth.VerifyEmailAsync(Verify(email, outbox.LastCode), CancellationToken.None));
            Assert.True(verified.IsSuccess, verified.Error?.Message);
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
                clock,
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

        private static VerifyEmailRequest Verify(string email, string code) => new() { Email = email, Code = code };

        private static ResendConfirmationRequest Resend(string email) => new() { Email = email };

        [GeneratedRegex(@"\b(\d{6})\b")]
        private static partial Regex SixDigits();

        private sealed class TestClock : TimeProvider
        {
            private TimeSpan offset = TimeSpan.Zero;

            public void Advance(TimeSpan by) => offset += by;

            public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow + offset;
        }

        private sealed class Outbox : IVerificationEmailSender
        {
            public List<EmailMessage> Sent { get; } = [];

            public bool Fail { get; set; }

            public string LastCode { get; private set; } = string.Empty;

            public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
            {
                if (Fail)
                    throw new InvalidOperationException("SMTP caído");

                Sent.Add(message);
                LastCode = SixDigits().Match(message.Subject).Groups[1].Value;
                return Task.CompletedTask;
            }
        }
    }
}
