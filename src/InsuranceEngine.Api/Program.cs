using System.Reflection;
using System.Text;
using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Insurance Engine API",
        Version = "v1",
        Description = "Dynamic formula-driven insurance calculation engine. " +
                      "Supports product management, formula evaluation, condition rules, " +
                      "traditional product calculations, and bulk Excel/CSV uploads.",
        Contact = new OpenApiContact
        {
            Name = "Insurance Engine",
            Url = new Uri("https://github.com/abhiezytek/insurance-engine")
        }
    });

    // Include XML comments from the generated documentation file
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);

    // Support multipart/form-data file uploads
    c.OperationFilter<InsuranceEngine.Api.Swagger.FileUploadOperationFilter>();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    if (builder.Environment.IsProduction())
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is not configured. " +
            "Set ConnectionStrings__DefaultConnection environment variable or use appsettings.Production.json.");

    // Local dev fallback only
    connectionString = "Server=localhost;Database=InsuranceEngineDb;TrustServerCertificate=True;Integrated Security=True";
}

builder.Services.AddDbContext<InsuranceDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<FormulaEngine>();
builder.Services.AddScoped<ConditionEvaluator>();
builder.Services.AddScoped<IBenefitCalculationService, BenefitCalculationService>();
builder.Services.AddScoped<IUlipCalculationService, UlipCalculationService>();

// Core system gateway: conditional registration (mock vs HTTP)
var coreSystemConfig = builder.Configuration.GetSection("CoreSystem");
builder.Services.Configure<CoreSystemConfig>(coreSystemConfig);

if (coreSystemConfig.GetValue<bool>("IsEnabled") && !coreSystemConfig.GetValue<bool>("UseMock"))
{
    builder.Services.AddHttpClient<ICoreSystemGateway, CoreSystemHttpGateway>();
}
else
{
    builder.Services.AddScoped<ICoreSystemGateway, MockCoreSystemGateway>();
}

builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPayoutService, PayoutService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActivityAuditService, ActivityAuditService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<InsuranceDbContext>("database");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[]
    {
        "http://insuranceengineui.ezytekapis.com",
        "https://insuranceengineui.ezytekapis.com"
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is not configured. Set Jwt:Key in appsettings or environment variables.");

if (jwtKey.Length < 32)
    throw new InvalidOperationException("JWT key must be at least 32 characters for HMAC-SHA256 security.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PrecisionPro";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PrecisionProUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    // Restrict temp (password-change-only) tokens to the change-password endpoint
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var scopeClaim = context.Principal?.FindFirst("scope")?.Value;
            if (scopeClaim == "password-change-only")
            {
                var path = context.HttpContext.Request.Path.Value ?? "";
                if (!path.Equals("/api/auth/change-password", StringComparison.OrdinalIgnoreCase))
                {
                    context.Fail("Temporary token can only be used for password change.");
                }
            }
            return Task.CompletedTask;
        }
    };
});

// Authorization policies based on JWT role claims
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanEditConfiguration", policy =>
        policy.RequireRole("Admin", "SuperAdmin", "Actuary"));

    options.AddPolicy("CanManageUsers", policy =>
        policy.RequireRole("Admin", "SuperAdmin"));

    options.AddPolicy("CanViewAudit", policy =>
        policy.RequireRole("Admin", "SuperAdmin", "Actuary", "AuditUser", "Auditor",
                           "Operations", "Checker", "Authorizer"));

    options.AddPolicy("CanViewBI", policy =>
        policy.RequireRole("Admin", "SuperAdmin", "Actuary", "Operations", "ReadOnly"));
});

var app = builder.Build();

// Global exception handler — ensures CORS headers are present even on 500 responses.
// Must be registered BEFORE CORS/routing so it wraps all middleware.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

        var logger = context.RequestServices.GetService<ILoggerFactory>()
            ?.CreateLogger("ExceptionHandler");
        if (error != null)
            logger?.LogError(error.Error, "Unhandled exception on {Path}", context.Request.Path);

        await context.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(new
            {
                status = 500,
                message = "An internal server error occurred."
            }));
    });
});

// Swagger is available in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Insurance Engine API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Insurance Engine API";
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
});

// CORS must come BEFORE routing so headers are present on all responses
// including error responses and preflight (OPTIONS) requests.
app.UseCors("AllowFrontend");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResultStatusCodes = {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapGet("/api/health", async (IServiceProvider sp, IConfiguration config, IWebHostEnvironment env, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("HealthCheck");
    var dbHealthy = false;
    var pendingMigrations = Array.Empty<string>();
    try
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InsuranceDbContext>();
        dbHealthy = await db.Database.CanConnectAsync();
        if (dbHealthy)
            pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToArray();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Health check: database connectivity test failed");
    }

    var jwtConfigured = !string.IsNullOrWhiteSpace(config["Jwt:Key"]) && config["Jwt:Key"]!.Length >= 32;
    var dbConfigured = !string.IsNullOrWhiteSpace(config.GetConnectionString("DefaultConnection"));
    var migrationsUpToDate = dbHealthy && pendingMigrations.Length == 0;
    var overallHealthy = dbHealthy && migrationsUpToDate && jwtConfigured && dbConfigured;

    var result = new
    {
        status = overallHealthy ? "healthy" : "unhealthy",
        timestamp = DateTime.UtcNow,
        version = "1.0.0",
        environment = env.EnvironmentName,
        checks = new
        {
            database = dbHealthy ? "connected" : "unreachable",
            migrationsUpToDate,
            pendingMigrationCount = pendingMigrations.Length,
            connectionStringConfigured = dbConfigured,
            jwtKeyConfigured = jwtConfigured,
            jwtIssuerConfigured = !string.IsNullOrWhiteSpace(config["Jwt:Issuer"]),
            jwtAudienceConfigured = !string.IsNullOrWhiteSpace(config["Jwt:Audience"])
        }
    };

    return overallHealthy ? Results.Ok(result) : Results.Json(result, statusCode: 503);
});

// Verify database connectivity and apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InsuranceDbContext>();
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        if (canConnect)
            app.Logger.LogInformation("Database connection verified");
        else
            app.Logger.LogCritical("Database connection failed: CanConnectAsync returned false");
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database connection failed: {Message}", ex.Message);
    }

    try
    {
        db.Database.Migrate();
        await SeedData.SeedAsync(db);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        if (app.Environment.IsProduction())
        {
            logger.LogCritical(ex, "Database migration/seed failed. Aborting startup — tables (e.g. Users) may not exist.");
            throw;
        }

        logger.LogWarning(ex, "Could not apply migrations/seed. Continuing startup (non-Production).");
    }
}

app.Run();

public partial class Program { }
