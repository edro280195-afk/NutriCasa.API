namespace NutriCasa.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string fullName);
    string GenerateRefreshToken();
    Guid? ValidateAccessToken(string token);
}
