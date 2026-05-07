using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

/// <summary>
/// Pendiente de Fase 8 — Integración de pagos.
/// </summary>
public class MercadoPagoServiceStub : IPaymentService
{
    public Task<string> CreateSubscriptionAsync(Guid userId, string planCode, CancellationToken ct = default)
        => throw new NotImplementedException("Pendiente de Fase 8 — Integración de pagos");

    public Task CancelSubscriptionAsync(string providerSubscriptionId, CancellationToken ct = default)
        => throw new NotImplementedException("Pendiente de Fase 8 — Integración de pagos");
}
