using System.Text;
using API.Furnistore.Application.Auth;
using API.Furnistore.API.Extensions;
using API.Furnistore.API.Middleware;
using API.Furnistore.API.Services;
using API.Furnistore.Data;
using dotenv.net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    if (builder.Environment.IsDevelopment())
    {
        DotEnv.Load(
            options: new DotEnvOptions(
                envFilePaths: new[] { "../.env" },
                overwriteExistingVars: false
            )
        );
    }

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddControllers();
    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        }
    );
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor,
        Microsoft.AspNetCore.Mvc.Infrastructure.ActionContextAccessor>();
    builder.Services.AddApplicationServices();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "furnistore_API", Version = "v1" });
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

    var connectionString =
        Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? builder.Configuration.GetConnectionString("APIFurnistoreContext")
        ?? throw new InvalidOperationException("DATABASE_URL not configured");

    builder.Services.AddDbContext<APIFurnistoreContext>(options =>
        options.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(2),
                    errorCodesToAdd: null
                );
                npgsql.CommandTimeout(20);
            }
        )
    );

    builder.Services.AddHostedService<DatabaseWarmupService>();

    //Configurar JWT con variables de entorno
    var jwtSecret =
        Environment.GetEnvironmentVariable("JWT_SECRET")
        ?? builder.Configuration["JwtConfig:Secret"]
        ?? throw new InvalidOperationException("JWT_SECRET not configured");

    var jwtIssuer =
        Environment.GetEnvironmentVariable("JWT_ISSUER")
        ?? builder.Configuration["JwtConfig:Issuer"]
        ?? throw new InvalidOperationException("JWT_ISSUER not configured");

    var jwtAudience =
        Environment.GetEnvironmentVariable("JWT_AUDIENCE")
        ?? builder.Configuration["JwtConfig:Audience"]
        ?? throw new InvalidOperationException("JWT_AUDIENCE not configured");

    builder.Services.AddSingleton(
        new JwtOptions
        {
            Secret = jwtSecret,
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            ExpiryTime = TimeSpan.Parse(builder.Configuration["JwtConfig:ExpiryTime"] ?? "01:00:00"),
        }
    );


    // Email
    builder.Services.Configure<API.Furnistore.API.Configuration.SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
    builder.Services.AddSingleton<IEmailSender, EmailService>();

    //JWT
    var key = Encoding.UTF8.GetBytes(jwtSecret); //Aqui se guarda el valor del secret jwt
    var tokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),

        //Esto en produccion debe ser verdadero, esto valida quien emitio el token
        // para asegurarse q no hubo nadie intermedio que cambiara el token
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,

        //Esto en produccion debe ser verdadero, que el destinatario
        // de este token debe ser el mismo que lo esta recibiendo
        ValidateAudience = true,
        ValidAudience = jwtAudience,

        RequireExpirationTime = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
    };

    builder.Services.AddSingleton(tokenValidationParameters);

    builder
        .Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(jwt =>
        {
            jwt.SaveToken = true;
            jwt.TokenValidationParameters = tokenValidationParameters;
        });

    builder
        .Services.AddDefaultIdentity<IdentityUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddEntityFrameworkStores<APIFurnistoreContext>();

    // NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();
    
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseExceptionHandler();

    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.LogRegisteredEndpoints();

    app.Run();
}
catch (Exception ex)
{
    //NLog: catch setup errors
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}
