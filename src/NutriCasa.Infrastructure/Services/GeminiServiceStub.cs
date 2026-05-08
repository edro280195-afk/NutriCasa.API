using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Infrastructure.Services;

public class GeminiServiceStub : IGeminiService
{
    public Task<GeneratePlanResponse> GeneratePlanAsync(GeneratePlanRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Migrado a GeminiService real. Eliminar este stub.");

    public Task<SwapMealResponse> SwapMealAsync(SwapMealRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Migrado a GeminiService real. Eliminar este stub.");
}
