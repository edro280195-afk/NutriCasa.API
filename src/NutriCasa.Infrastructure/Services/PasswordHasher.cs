using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    public bool VerifyPassword(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
