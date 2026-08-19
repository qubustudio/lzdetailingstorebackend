using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Infrastructure.Auth;
using LzDetailingStore.Infrastructure.Persistence;
using LzDetailingStore.Infrastructure.Persistence.Repositories;
using LzDetailingStore.Infrastructure.Services;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LzDetailingStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Auth services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        // Catalog services
        services.AddScoped<ISlugGenerator, SlugGenerator>();
        services.AddScoped<IS3Service, S3Service>();

        // Commerce services
        services.AddScoped<IMercadoPagoService, MercadoPagoService>();
        services.AddHttpClient("MercadoPago", (sp, client) =>
        {
            client.BaseAddress = new Uri("https://api.mercadopago.com/");
            var token = configuration["MercadoPago:AccessToken"] ?? string.Empty;
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        });

        // Content services
        services.AddScoped<IBanxicoService, BanxicoService>();
        services.AddHttpClient("Banxico");
        services.AddHostedService<LzDetailingStore.Infrastructure.Jobs.ExchangeRateJob>();

        // Intelligence services (embeddings + recommendations)
        services.Configure<AI.EmbeddingOptions>(configuration.GetSection("Embedding"));
        services.AddSingleton<IEmbeddingService, AI.OnnxEmbeddingService>();
        services.AddScoped<IEmbeddingJobService, Jobs.RegenerateEmbeddingJob>();

        // Admin operations (bulk import/export)
        services.AddScoped<IBulkImportService, ExcelBulkImportService>();
        services.AddScoped<IBulkExportService, ExcelBulkExportService>();

        // Audit
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
