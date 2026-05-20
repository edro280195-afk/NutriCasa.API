using Microsoft.Extensions.Configuration;
using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

public class FeatureFlagService : IFeatureFlagService
{
    private readonly IConfiguration _configuration;

    public FeatureFlagService(IConfiguration configuration) => _configuration = configuration;

    // Default true durante la fase de arranque. Para reactivar los límites,
    // poner "Features:UnlimitedRegeneration": false en appsettings.
    public bool UnlimitedRegeneration =>
        _configuration.GetValue("Features:UnlimitedRegeneration", true);
}
