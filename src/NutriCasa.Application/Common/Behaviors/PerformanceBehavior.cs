using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace NutriCasa.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior que alerta cuando un request tarda más de 500ms.
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int WarningThresholdMs = 500;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        TResponse response = await next();
        stopwatch.Stop();

        long elapsed = stopwatch.ElapsedMilliseconds;
        if (elapsed > WarningThresholdMs)
        {
            string requestName = typeof(TRequest).Name;
            _logger.LogWarning(
                "Request lento detectado: {RequestName} tardó {ElapsedMs}ms (umbral: {Threshold}ms)",
                requestName, elapsed, WarningThresholdMs);
        }

        return response;
    }
}
