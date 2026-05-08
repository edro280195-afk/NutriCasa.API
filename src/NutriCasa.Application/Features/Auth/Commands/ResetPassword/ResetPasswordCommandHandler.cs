using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Auth.Commands.RegisterUser;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        string tokenHash = RegisterUserCommandHandler.ComputeSha256Hash(request.Token);

        // 1. Buscar token válido (no usado, no expirado)
        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                t.UsedAt == null &&
                t.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        if (resetToken is null)
            return Result.Failure(
                "El enlace de restablecimiento no es válido o ha expirado.", "INVALID_RESET_TOKEN");

        var user = resetToken.User;

        // 2. Hash de la nueva contraseña
        string newHash = _passwordHasher.HashPassword(request.NewPassword);
        user.PasswordHash = newHash;

        // 3. Marcar token como usado
        resetToken.UsedAt = DateTime.UtcNow;

        // 4. Revocar TODOS los refresh tokens del usuario
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var rt in activeTokens)
            rt.RevokedAt = DateTime.UtcNow;

        // 5. Registrar en audit_log
        var auditEntry = new AuditLog
        {
            Id         = Guid.NewGuid(),
            UserId     = user.Id,
            EntityType = "users",
            EntityId   = user.Id,
            Action     = "password_changed",
            NewValues  = JsonSerializer.Serialize(new { changed_at = DateTime.UtcNow }),
            IpAddress  = System.Net.IPAddress.TryParse(_currentUserService.IpAddress, out var ip) ? ip : null,
            UserAgent  = _currentUserService.UserAgent,
            CreatedAt  = DateTime.UtcNow,
        };
        _context.AuditLogs.Add(auditEntry);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
