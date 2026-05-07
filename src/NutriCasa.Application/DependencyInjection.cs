using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NutriCasa.Application.Common.Behaviors;
using NutriCasa.Application.Common.Mappings;

namespace NutriCasa.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // Pipeline behaviors en orden: Logging → Validation → Performance
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        MapsterConfig.Configure();

        return services;
    }
}
