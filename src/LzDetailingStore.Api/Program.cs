using System.Security.Claims;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;
using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Api.Middleware;
using LzDetailingStore.Application;
using LzDetailingStore.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// OpenAPI + Scalar
builder.Services.AddOpenApi();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Memory Cache
builder.Services.AddMemoryCache();

// App Settings
builder.Services.Configure<LzDetailingStore.Application.Features.Commerce.Commands.CreateCheckout.AppSettings>(
    builder.Configuration.GetSection("App"));

// JWT Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:3000", "http://localhost:4200"];

        policy.WithOrigins(origins)
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", "X-Correlation-ID")
            .WithExposedHeaders("X-Correlation-ID", "X-RateLimit-Remaining")
            .AllowCredentials();
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "RATE_LIMITED",
            message = "Demasiadas solicitudes. Intenta de nuevo en un momento.",
            statusCode = 429
        }, ct);
    };

    options.AddFixedWindowLimiter("public", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("authenticated", opt =>
    {
        opt.PermitLimit = 200;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("admin", opt =>
    {
        opt.PermitLimit = 500;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("LzDetailingStore.Api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint));
        }
    });

// Hangfire
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(opts =>
        opts.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")!)));
builder.Services.AddHangfireServer();

// HttpClient for external services
builder.Services.AddHttpClient();

var app = builder.Build();

// Seed data
await LzDetailingStore.Infrastructure.Persistence.Seed.SeedData.InitializeAsync(app.Services);

// Middleware pipeline (order matters!)
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// OpenAPI + Scalar + Hangfire Dashboard (dev only)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapHangfireDashboard("/hangfire");
}

// Health checks
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready");

// Endpoints
app.MapApiEndpoints();

app.Run();
