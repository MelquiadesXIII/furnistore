using System.Diagnostics;
using API.Furnistore.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.API.Middleware
{
    public sealed class RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger
    )
    {
        private const int SlowRequestThresholdMs = 1000;

        private static readonly string[] RedactedQueryKeys = ["code", "token", "refreshToken", "password"];

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            using var scope = logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["TraceId"] = context.TraceIdentifier,
                    ["UserId"] = context.User.FindFirst("Id")?.Value ?? "anonymous",
                }
            );

            try
            {
                await next(context);
            }
            finally
            {
                stopwatch.Stop();

                var elapsedMs = stopwatch.ElapsedMilliseconds;
                var slow = elapsedMs > SlowRequestThresholdMs;

                logger.Log(
                    slow ? LogLevel.Warning : LogLevel.Information,
                    slow ? ApiEvents.SlowRequest : ApiEvents.RequestCompleted,
                    "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path + RedactQuery(context.Request.Query),
                    context.Response.StatusCode,
                    elapsedMs
                );
            }
        }

        private static string RedactQuery(IQueryCollection query)
        {
            if (query.Count == 0)
                return string.Empty;

            var parts = query.Select(pair =>
                RedactedQueryKeys.Contains(pair.Key, StringComparer.OrdinalIgnoreCase)
                    ? $"{pair.Key}=***"
                    : $"{pair.Key}={pair.Value}"
            );

            return "?" + string.Join("&", parts);
        }
    }
}
