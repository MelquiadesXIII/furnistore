using System.Globalization;
using System.Threading.RateLimiting;
using API.Furnistore.Application.Common;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Furnistore.API.Extensions
{
    public static class AuthRateLimits
    {
        public const string Credentials = "auth-credentials";

        public const string Session = "auth-session";
    }

    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services) =>
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(AuthRateLimits.Credentials, context => PerClient(context, permitsPerMinute: 10));
                options.AddPolicy(AuthRateLimits.Session, context => PerClient(context, permitsPerMinute: 30));
                options.OnRejected = RejectAsync;
            });

        private static RateLimitPartition<string> PerClient(HttpContext context, int permitsPerMinute) =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }
            );

        private static async ValueTask RejectAsync(OnRejectedContext context, CancellationToken cancellationToken)
        {
            var http = context.HttpContext;

            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                http.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                    .ToString(CultureInfo.InvariantCulture);

            http.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("API.Furnistore.API.RateLimiting")
                .LogWarning(ApiEvents.RequestRateLimited, "Rate limit hit on {Method} {Path}", http.Request.Method, http.Request.Path);

            await TypedResults
                .Problem(
                    title: "Demasiados intentos. Espera un momento e intenta de nuevo.",
                    statusCode: StatusCodes.Status429TooManyRequests,
                    extensions: new Dictionary<string, object?> { ["code"] = "rate_limited" }
                )
                .ExecuteAsync(http);
        }
    }
}
