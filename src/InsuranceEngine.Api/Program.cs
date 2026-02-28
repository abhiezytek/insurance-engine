using System.Reflection;
using InsuranceEngine.Api.Data;
using InsuranceEngine.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
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
    ?? "Server=localhost;Database=InsuranceEngineDb;TrustServerCertificate=True;Integrated Security=True";

builder.Services.AddDbContext<InsuranceDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<FormulaEngine>();
builder.Services.AddScoped<ConditionEvaluator>();
builder.Services.AddScoped<IBenefitCalculationService, BenefitCalculationService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<InsuranceDbContext>("database");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

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

app.UseCors();
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
}

app.Run();

public partial class Program { }
