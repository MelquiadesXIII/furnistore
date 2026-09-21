using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

namespace API.Furnistore.API.Extensions
{
    public static class ApiContractExtensions
    {
        public static IMvcBuilder AddApiContract(this IServiceCollection services)
        {
            var mvc = services
                .AddControllers()
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
                );

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "furnistore_API", Version = "v1" });
                c.SupportNonNullableReferenceTypes();
                c.UseAllOfToExtendReferenceSchemas();
                c.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme()
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description =
                            "JWT Authorization header using the Bearer scheme. \n\n Enter prefix (Bearer), space, and then your token. Example 'Bearer 2287386hfdfhj'",
                    }
                );
                c.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer",
                                },
                            },
                            new string[] { }
                        },
                    }
                );
            });

            return mvc;
        }
    }
}
