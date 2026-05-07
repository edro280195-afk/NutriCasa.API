using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Infrastructure.BackgroundJobs;
using NutriCasa.Infrastructure.Persistence;
using NutriCasa.Infrastructure.Persistence.Interceptors;
using NutriCasa.Infrastructure.Services;

namespace NutriCasa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Interceptors
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        // DbContext
        string connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention();
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Servicios reales
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // Stubs (se reemplazan en fases posteriores)
        services.AddScoped<IFileStorageService, CloudflareR2StorageService>();
        services.AddScoped<IGeminiService, GeminiServiceStub>();
        services.AddScoped<IPlanValidator, PlanValidatorStub>();
        services.AddScoped<ICostEstimationService, CostEstimationServiceStub>();
        services.AddScoped<IIngredientSubstitutionService, IngredientSubstitutionServiceStub>();
        services.AddScoped<IModerationService, ModerationServiceStub>();
        services.AddScoped<IPaymentService, MercadoPagoServiceStub>();

        // Hangfire
        services.AddHangfireServices(configuration);

        return services;
    }
}
