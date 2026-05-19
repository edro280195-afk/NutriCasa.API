using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

public class MercadoPagoPaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _context;

    public MercadoPagoPaymentService(
        HttpClient httpClient,
        IConfiguration configuration,
        IApplicationDbContext context)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _context = context;
    }

    public async Task<PaymentCheckoutResult> CreateCheckoutSessionAsync(
        Guid userId,
        Guid planId,
        string returnUrl,
        CancellationToken ct = default)
    {
        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.IsActive, ct);

        if (plan is null)
            throw new InvalidOperationException("Plan no encontrado para generar checkout.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            throw new InvalidOperationException("Usuario no encontrado para generar checkout.");

        var frontendBaseUrl = (_configuration["App:FrontendBaseUrl"] ?? "https://nutricasa.app").TrimEnd('/');
        var notificationUrl = _configuration["MercadoPago:NotificationUrl"];
        var externalReference = $"subscription:{userId:N}:{planId:N}:{Guid.NewGuid():N}";
        var resolvedReturnUrl = ResolveReturnUrl(frontendBaseUrl, returnUrl);

        var body = new
        {
            items = new[]
            {
                new
                {
                    id = plan.Id.ToString(),
                    title = $"NutriCasa {plan.Name}",
                    description = plan.Description,
                    quantity = 1,
                    currency_id = "MXN",
                    unit_price = plan.PriceMonthlyMxn
                }
            },
            payer = new
            {
                name = user.FullName,
                email = user.Email
            },
            external_reference = externalReference,
            back_urls = new
            {
                success = resolvedReturnUrl,
                failure = $"{frontendBaseUrl}/profile/subscription?payment=failure",
                pending = $"{frontendBaseUrl}/profile/subscription?payment=pending"
            },
            auto_return = "approved",
            notification_url = string.IsNullOrWhiteSpace(notificationUrl) ? null : notificationUrl,
            metadata = new
            {
                user_id = userId.ToString(),
                plan_id = planId.ToString()
            }
        };

        using var response = await _httpClient.PostAsJsonAsync("checkout/preferences", body, ct);
        var result = await response.Content.ReadFromJsonAsync<MercadoPagoPreferenceResponse>(cancellationToken: ct);

        if (!response.IsSuccessStatusCode || result is null || string.IsNullOrWhiteSpace(result.Id))
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"MercadoPago no pudo crear la preferencia: {response.StatusCode} - {error}");
        }

        var useSandbox = _configuration.GetValue("MercadoPago:UseSandboxInitPoint", false);
        var checkoutUrl = useSandbox && !string.IsNullOrWhiteSpace(result.SandboxInitPoint)
            ? result.SandboxInitPoint
            : result.InitPoint;

        if (string.IsNullOrWhiteSpace(checkoutUrl))
            throw new InvalidOperationException("MercadoPago no devolvio URL de checkout.");

        return new PaymentCheckoutResult(result.Id, checkoutUrl);
    }

    public Task<string> CreateTrialSubscriptionAsync(Guid userId, Guid planId, CancellationToken ct = default)
    {
        return Task.FromResult($"trial_{userId:N}_{planId:N}_{DateTime.UtcNow:yyyyMMddHHmmss}");
    }

    public Task CancelSubscriptionAsync(string providerSubscriptionId, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public async Task<bool> VerifyPaymentAsync(string providerPaymentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
            return false;

        using var response = await _httpClient.GetAsync($"v1/payments/{Uri.EscapeDataString(providerPaymentId)}", ct);
        if (!response.IsSuccessStatusCode)
            return false;

        var payment = await response.Content.ReadFromJsonAsync<MercadoPagoPaymentResponse>(cancellationToken: ct);
        return string.Equals(payment?.Status, "approved", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveReturnUrl(string frontendBaseUrl, string returnUrl)
    {
        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out _))
            return returnUrl;

        return $"{frontendBaseUrl}/{returnUrl.TrimStart('/')}";
    }

    private sealed record MercadoPagoPreferenceResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("init_point")]
        public string? InitPoint { get; init; }

        [JsonPropertyName("sandbox_init_point")]
        public string? SandboxInitPoint { get; init; }
    }

    private sealed record MercadoPagoPaymentResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; init; }
    }
}
