using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Auth.DTOs;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.Auth.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    public RegisterUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IEmailService emailService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
    }

    public async Task<Result<RegisterResponse>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar unicidad de email (CITEXT en BD: case-insensitive automático)
        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (emailExists)
            return Result<RegisterResponse>.Failure(
                "Ya existe una cuenta con este email.", "EMAIL_TAKEN");

        // 2. Leer expiración de token desde system_thresholds
        int expiryHours = await GetThresholdHoursAsync("email_verification_expiry_hours", 24, cancellationToken);

        // 3. Generar token de verificación
        string rawToken  = Guid.NewGuid().ToString("N"); // 32 chars hex
        string tokenHash = ComputeSha256Hash(rawToken);

        // 4. Hash de contraseña
        string passwordHash = _passwordHasher.HashPassword(request.Password);

        // 5. Crear usuario
        var user = new User
        {
            Id                      = Guid.NewGuid(),
            FullName                = request.FullName.Trim(),
            Email                   = request.Email.ToLowerInvariant(),
            PasswordHash            = passwordHash,
            BirthDate               = request.BirthDate,
            EmailVerificationToken  = tokenHash,
            EmailVerifiedAt         = null,
        };

        // El EmailVerificationToken almacena el HASH — el raw token viaja solo en el email
        // Usamos el campo TextValue del threshold para la expiración si existiera, pero
        // simplemente añadimos horas al created_at en la verificación.
        // Guardamos la hora en un campo calculable: usamos CreatedAt + threshold para verificar.

        // 6. Crear privacy_settings con defaults (obligatorio 1:1)
        var privacySettings = new PrivacySettings
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.Users.Add(user);
        _context.PrivacySettings.Add(privacySettings);
        await _context.SaveChangesAsync(cancellationToken);

        // 7. Enviar email de verificación con el raw token (no el hash)
        // Si falla el email, auto-verificamos al usuario para no bloquear su registro
        string verificationLink = $"https://nutricasa.app/verify-email?token={rawToken}";
        try
        {
            await _emailService.SendEmailVerificationAsync(
                user.Email,
                user.FullName,
                verificationLink,
                cancellationToken);
        }
        catch
        {
            user.EmailVerifiedAt = DateTime.UtcNow;
            user.EmailVerificationToken = null;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result<RegisterResponse>.Success(new RegisterResponse());
    }

    private async Task<int> GetThresholdHoursAsync(
        string code, int defaultValue, CancellationToken ct)
    {
        var threshold = await _context.SystemThresholds
            .FirstOrDefaultAsync(t => t.Code == code && t.IsActive, ct);
        return (int)(threshold?.NumericValue ?? defaultValue);
    }

    internal static string ComputeSha256Hash(string rawData)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
