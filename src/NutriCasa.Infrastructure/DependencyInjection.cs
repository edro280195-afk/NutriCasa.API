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

        // Push notifications
        services.AddSingleton<IPushNotificationService, PushNotificationService>();

        // Servicios reales de Fase 1
        // IFileStorageService registrado en Program.cs (LocalFileStorageService con web root path)
        services.AddScoped<IGeminiService, GeminiService>();
        services.AddScoped<IPlanValidator, PlanValidator>();
        services.AddScoped<ICostEstimationService, CostEstimationService>();
        services.AddScoped<IIngredientSubstitutionService, IngredientSubstitutionService>();
        services.AddScoped<IModerationService, ModerationServiceStub>();
        services.AddScoped<IPaymentService, MercadoPagoServiceStub>();
        services.AddScoped<IEmailService, ResendEmailService>();

        // Hangfire
        services.AddHangfireServices(configuration);

        return services;
    }
}
