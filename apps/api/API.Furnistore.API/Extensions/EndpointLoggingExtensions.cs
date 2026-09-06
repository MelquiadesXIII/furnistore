using API.Furnistore.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace API.Furnistore.API.Extensions
{
    public static class EndpointLoggingExtensions
    {
        public static WebApplication LogRegisteredEndpoints(this WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
                return app;

            var logger = app
                .Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("API.Furnistore.API.Endpoints");

            var actions = app
                .Services.GetRequiredService<IActionDescriptorCollectionProvider>()
                .ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
                .Where(action => action.AttributeRouteInfo?.Template is not null)
                .Select(action => new
                {
                    Controller = action.ControllerName,
                    Method = HttpMethodOf(action),
                    Route = "/" + action.AttributeRouteInfo!.Template!.TrimStart('/'),
                    Access = AccessOf(action),
                })
                .OrderBy(action => action.Controller)
                .ThenBy(action => action.Route)
                .ThenBy(action => action.Method)
                .ToList();

            logger.LogInformation(
                ApiEvents.EndpointsRegistered,
                "{EndpointCount} endpoints registrados en {ControllerCount} controladores",
                actions.Count,
                actions.Select(a => a.Controller).Distinct().Count()
            );

            foreach (var group in actions.GroupBy(action => action.Controller))
            {
                logger.LogInformation("  {Controller}", group.Key);

                foreach (var action in group)
                    logger.LogInformation(
                        "    {Method,-6} {Route,-42} {Access}",
                        action.Method,
                        action.Route,
                        action.Access
                    );
            }

            return app;
        }

        private static string HttpMethodOf(ControllerActionDescriptor action)
        {
            var methods = action
                .ActionConstraints?.OfType<HttpMethodActionConstraint>()
                .FirstOrDefault()
                ?.HttpMethods;

            return methods is null ? "ANY" : string.Join("|", methods);
        }

        private static string AccessOf(ControllerActionDescriptor action)
        {
            if (action.EndpointMetadata.OfType<IAllowAnonymous>().Any())
                return "publico";

            return action.EndpointMetadata.OfType<IAuthorizeData>().Any() ? "JWT" : "publico";
        }
    }
}
