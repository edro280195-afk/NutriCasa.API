namespace NutriCasa.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
