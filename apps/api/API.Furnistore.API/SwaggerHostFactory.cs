using API.Furnistore.API.Extensions;

namespace API.Furnistore.API
{
    public static class SwaggerHostFactory
    {
        public static IHost CreateHost()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddApiContract().AddApplicationPart(typeof(SwaggerHostFactory).Assembly);
            return builder.Build();
        }
    }
}
