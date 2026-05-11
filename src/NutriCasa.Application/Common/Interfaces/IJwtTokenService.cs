namespace NutriCasa.Application.Common.Interfaces;

public interface IJwtTokenService
{
    /// <summary>
    /// Genera un access token JWT con todos los claims de NutriCasa.
    /// </summary>
    string GenerateAccessToken(
        Guid userId,
        string email,
        string fullName,
        bool emailVerified,
        bool onboardingComplete,
        string role);

    string GenerateRefreshToken();
    Guid? ValidateAccessToken(string token);
}
