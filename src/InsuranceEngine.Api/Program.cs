using System.Reflection;
using System.Text;
using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.Exceptions;
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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=SQL1001.site4now.net;Initial Catalog=db_abd5c4_insuranceenginedb;User Id=db_abd5c4_insuranceenginedb_admin;Password=Insurance@#123;Encrypt=True;TrustServerCertificate=True;";

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

// Global exception handler — maps typed product exceptions to appropriate HTTP
// status codes and ensures CORS headers are present even on error responses.
// Must be registered BEFORE CORS/routing so it wraps all middleware.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetService<ILoggerFactory>()
            ?.CreateLogger("ExceptionHandler");

        if (error?.Error is ProductValidationException validationEx)
        {
            // Client sent an invalid request — 400 Bad Request
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = 400,
                    message = validationEx.Message
                }));
            return;
        }

        if (error?.Error is ProductRuleNotFoundException ruleEx)
        {
            // IsConfigGap = true → internal config issue (500)
            // IsConfigGap = false → unsupported request combination (400)
            var statusCode = ruleEx.IsConfigGap ? 500 : 400;
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            var message = ruleEx.IsConfigGap
                ? "A required product rule or factor is not configured. Please contact support."
                : ruleEx.Message;
            if (ruleEx.IsConfigGap)
                logger?.LogError(ruleEx, "Product rule config gap on {Path}", context.Request.Path);
            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = statusCode,
                    message
                }));
            return;
        }

        if (error?.Error is ProductConfigurationException configEx)
        {
            // Internal configuration failure — 500
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            logger?.LogError(configEx, "Product configuration error on {Path}", context.Request.Path);
            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = 500,
                    message = "A product configuration error occurred. Please contact support."
                }));
            return;
        }

        // Unknown / unhandled exception — generic 500 (no stack trace exposed)
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
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

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

// Apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InsuranceDbContext>();
    try
    {
        db.Database.Migrate();
        await SeedData.SeedAsync(db);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not apply migrations/seed. Continuing startup.");
    }

    // ── Startup fail-fast: validate critical product configuration ──
    // Missing factor tables or rule CSVs should prevent the app from serving
    // requests that would produce silent miscalculations.
    try
    {
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Century Income GSV factors
        var gsvCount = db.GsvFactors.Count();
        if (gsvCount == 0)
            startupLogger.LogWarning("Century Income GSV factor table is empty. GSV calculations will fail until factors are seeded.");

        // Century Income SSV factors
        var ssvCount = db.SsvFactors.Count();
        if (ssvCount == 0)
            startupLogger.LogWarning("Century Income SSV factor table is empty. SSV calculations will fail until factors are seeded.");

        // e-Wealth Royale PPT/PT rules
        var pptPtRules = PptPtRuleBook.Rules;
        if (pptPtRules.Count == 0)
            startupLogger.LogWarning("e-Wealth Royale PPT/PT rules are not loaded. ULIP calculations will fail until ewealth_ppt_pt_rules.csv is available.");

        // e-Wealth Royale risk preference rules
        var riskRules = RiskPreferenceRuleBook.Rules;
        if (riskRules.Count == 0)
            startupLogger.LogWarning("e-Wealth Royale risk preference rules are not loaded. Strategy validation will fail until ewealth_risk_preference_rules.csv is available.");
    }
    catch (Exception ex)
    {
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        startupLogger.LogWarning(ex, "Startup validation check encountered an error. Continuing startup.");
    }
}

app.Run();

public partial class Program { }
