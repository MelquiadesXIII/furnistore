using API.Furnistore.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Extensions
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
            result.IsSuccess ? controller.Ok(result.Value) : Problem(result.Error!, controller);

        public static IActionResult ToNoContentResult(this Result result, ControllerBase controller) =>
            result.IsSuccess ? controller.NoContent() : Problem(result.Error!, controller);

        public static IActionResult ToEmailConfirmationResult(
            this Result result,
            ControllerBase controller
        ) =>
            result.IsSuccess
                ? controller.Content("Thanks you for confirming your email.", "text/plain")
                : Problem(result.Error!, controller);

        private static IActionResult Problem(Error error, ControllerBase controller) =>
            controller.Problem(
                title: error.Message,
                statusCode: StatusFor(error.Type),
                extensions: new Dictionary<string, object?> { ["code"] = error.Code }
            );

        private static int StatusFor(ErrorType type) =>
            type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError,
            };
    }
}
