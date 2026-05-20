namespace NutriCasa.Application.Common.Interfaces;

public interface IFeatureFlagService
{
    /// <summary>
    /// Fase de arranque: cuando está activo, no se aplican los topes de
    /// regeneración de plan ni de sustitución de comidas (swaps ilimitados).
    /// Controlado por la clave de configuración "Features:UnlimitedRegeneration".
    /// </summary>
    bool UnlimitedRegeneration { get; }
}
