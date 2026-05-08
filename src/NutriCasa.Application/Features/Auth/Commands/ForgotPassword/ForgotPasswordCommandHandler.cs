using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Auth.Commands.RegisterUser;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<Result> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        // Respuesta idéntica sin importar si el email existe (no revelar existencia)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (user is not null)
        {
            int expiryHours = await GetThresholdAsync("password_reset_expiry_hours", 1, cancellationToken);

            string rawToken  = Guid.NewGuid().ToString("N");
            string tokenHash = RegisterUserCommandHandler.ComputeSha256Hash(rawToken);

            var resetToken = new PasswordResetToken
            {
                Id        = Guid.NewGuid(),
                UserId    = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
                CreatedAt = DateTime.UtcNow,
            };
            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync(cancellationToken);

            string resetLink = $"https://nutricasa.app/reset-password?token={rawToken}";
            try
            {
                await _emailService.SendPasswordResetAsync(
                    user.Email, user.FullName, resetLink, cancellationToken);
            }
            catch { /* no revelar errores de email */ }
        }

        // Siempre éxito para no revelar existencia del email
        return Result.Success();
    }

    private async Task<int> GetThresholdAsync(string code, int def, CancellationToken ct)
    {
        var t = await _context.SystemThresholds
            .FirstOrDefaultAsync(x => x.Code == code && x.IsActive, ct);
        return (int)(t?.NumericValue ?? def);
    }
}
