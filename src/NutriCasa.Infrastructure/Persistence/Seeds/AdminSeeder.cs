using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Infrastructure.Persistence.Seeds;

public static class AdminSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        if (await context.Users.AnyAsync(u => u.Role == "admin")) return;

        var adminEmail = "admin@nutricasa.app";

        if (await context.Users.AnyAsync(u => u.Email == adminEmail)) return;

        var admin = new User
        {
            FullName = "Admin NutriCasa",
            Email = adminEmail,
            PasswordHash = passwordHasher.HashPassword("Admin123!"),
            BirthDate = new DateOnly(1990, 1, 1),
            Gender = Gender.PreferNotToSay,
            HeightCm = 170,
            ActivityLevel = ActivityLevel.Sedentary,
            Role = "admin",
            EmailVerifiedAt = DateTime.UtcNow,
            DisclaimerAcceptedAt = DateTime.UtcNow,
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}
