using API.Furnistore.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.API.Middleware
{
    public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken
        )
        {
            if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
                return false;

            logger.LogError(
                ApiEvents.UnhandledException,
                exception,
                "Unhandled exception on {Method} {Path} [{TraceId}]",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier
            );

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Ocurrió un error inesperado.",
                Instance = $"{context.Request.Method} {context.Request.Path}",
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = problem.Status.Value;
            await context.Response.WriteAsJsonAsync(problem, cancellationToken);

            return true;
        }
    }
}
