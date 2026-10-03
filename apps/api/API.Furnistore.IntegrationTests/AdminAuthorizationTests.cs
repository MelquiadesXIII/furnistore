using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace API.Furnistore.IntegrationTests
{
    public sealed class AdminAuthorizationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
    {
        private const string Secret = "authorization-tests-secret-with-32-bytes-at-least";
        private const string Issuer = "furnistore-tests";

        [Fact]
        public async Task Every_admin_endpoint_rejects_anonymous_callers_and_customers()
        {
            await using var factory = CreateFactory();
            var endpoints = AdminEndpoints(factory);
            Assert.True(endpoints.Count >= 20, $"Only {endpoints.Count} admin endpoints found");

            using var client = factory.CreateClient();

            foreach (var (method, path) in endpoints)
            {
                var anonymous = await SendAsync(client, method, path, token: null);
                Assert.True(anonymous == HttpStatusCode.Unauthorized, $"{method} {path} as anonymous returned {anonymous}");

                var customer = await SendAsync(client, method, path, Token("User"));
                Assert.True(customer == HttpStatusCode.Forbidden, $"{method} {path} as customer returned {customer}");

                var admin = await SendAsync(client, method, path, Token("Admin"));
                Assert.True(
                    admin is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.InternalServerError),
                    $"{method} {path} as admin returned {admin}"
                );
            }
        }

        [Fact]
        public async Task Admin_only_endpoints_live_under_the_admin_prefix()
        {
            await using var factory = CreateFactory();

            var misplaced = factory
                .Services.GetRequiredService<EndpointDataSource>()
                .Endpoints.OfType<RouteEndpoint>()
                .Where(endpoint =>
                    endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(data => data.Roles?.Contains("Admin") == true)
                    && !endpoint.RoutePattern.RawText!.StartsWith("api/admin/")
                )
                .Select(endpoint => endpoint.RoutePattern.RawText)
                .ToList();

            Assert.Empty(misplaced);
        }

        private WebApplicationFactory<Program> CreateFactory()
        {
            Environment.SetEnvironmentVariable("DATABASE_URL", fixture.ConnectionString);
            Environment.SetEnvironmentVariable("JWT_SECRET", Secret);
            Environment.SetEnvironmentVariable("JWT_ISSUER", Issuer);
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", Issuer);

            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
        }

        private static List<(string Method, string Path)> AdminEndpoints(WebApplicationFactory<Program> factory) =>
            factory
                .Services.GetRequiredService<EndpointDataSource>()
                .Endpoints.OfType<RouteEndpoint>()
                .Where(endpoint => endpoint.RoutePattern.RawText!.StartsWith("api/admin"))
                .SelectMany(endpoint =>
                    (endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"]).Select(method =>
                        (method, "/" + endpoint.RoutePattern.RawText!.Replace("{id:int}", "1"))
                    )
                )
                .ToList();

        private static async Task<HttpStatusCode> SendAsync(HttpClient client, string method, string path, string? token)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);

            if (method is "POST" or "PUT")
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            if (token is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }

        private static string Token(string role)
        {
            var handler = new JwtSecurityTokenHandler();
            var now = DateTime.UtcNow;

            return handler.WriteToken(
                handler.CreateToken(
                    new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(
                            [
                                new Claim("Id", $"tests-{role}"),
                                new Claim(JwtRegisteredClaimNames.Email, $"{role}@example.com"),
                                new Claim(ClaimTypes.Role, role),
                            ]
                        ),
                        IssuedAt = now,
                        NotBefore = now,
                        Expires = now.AddMinutes(5),
                        Issuer = Issuer,
                        Audience = Issuer,
                        SigningCredentials = new SigningCredentials(
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
                            SecurityAlgorithms.HmacSha256
                        ),
                    }
                )
            );
        }
    }
}
