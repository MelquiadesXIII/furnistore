using System.Net;
using System.Text;
using API.Furnistore.Application.Auth;
using API.Furnistore.Application.Common;
using API.Furnistore.API.Commands;
using API.Furnistore.API.Configuration;
using API.Furnistore.API.Extensions;
using API.Furnistore.API.Middleware;
using API.Furnistore.API.Services;
using API.Furnistore.Data;
using dotenv.net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Esto es para evitar que el CORS nos bloquee cuando tengamos una pantalla admin
    var corsOrigins = (Environment.GetEnvironmentVariable("CORS_ORIGINS")
    ?? "http://localhost:3000,http://localhost:3001")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("WebApp", policy =>
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
    });

    if (builder.Environment.IsDevelopment())
    {
        // le puse estas 3 rutas porque con la ruta que necesitaba estaba dando bateo
                // y entonces volvi a poner como lo encontre y decidi poner ambas para evitar
                // que se rompa en alguna PC
        DotEnv.Load(
            options: new DotEnvOptions(
                envFilePaths: new[] { ".env", "../.env", "../../.env" },
                overwriteExistingVars: false
            )
        );
        builder.Configuration.AddEnvironmentVariables();
    }

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddApiContract();
    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        }
    );
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddApplicationServices();
    builder.Services.AddHealthChecks()
    .AddDbContextCheck<APIFurnistoreContext>("database", tags: ["db"]);



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
    builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
        var knownProxies = builder.Configuration["ForwardedHeaders:KnownProxies"] ?? string.Empty;
        foreach (var proxy in knownProxies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            options.KnownProxies.Add(IPAddress.Parse(proxy));
    });

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

    
    builder.Services.AddAuthRateLimiting();

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
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        })
        // La Linea de abajo es para el tema de los roles en la pagina web.
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<APIFurnistoreContext>();

    // NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    var app = builder.Build();

    var smtp = app.Services.GetRequiredService<IOptions<SmtpSettings>>().Value;
    if (smtp.Security != MailKit.Security.SecureSocketOptions.None && !smtp.HasCredentials)
        app.Logger.LogWarning(
            ApiEvents.SmtpNotConfigured,
            "SMTP sin credenciales: los correos de confirmación no se van a enviar. Define SmtpSettings__UserName y SmtpSettings__Password en apps/api/.env"
        );

    // Esto es para la asignacion del rol.
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        if (!await roleManager.RoleExistsAsync("User"))
            await roleManager.CreateAsync(new IdentityRole("User"));
    }
    if (await AdminCommand.TryRunAsync(app, args))
        return;

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseForwardedHeaders();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseExceptionHandler();

    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    app.UseRouting();
    app.UseCors("WebApp");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.MapControllers();

    app.LogRegisteredEndpoints();
    // Es la forma que tenemos de saber si esta vivo o no el servidor
    app.MapHealthChecks("/health");
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

public partial class Program;
