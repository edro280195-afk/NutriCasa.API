using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Auth.Commands.RegisterUser;
using NutriCasa.Application.Features.Auth.DTOs;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<AuthTokenResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUserService _currentUserService;

    public VerifyEmailCommandHandler(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AuthTokenResponse>> Handle(
        VerifyEmailCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Hash SHA-256 del token recibido
        string tokenHash = RegisterUserCommandHandler.ComputeSha256Hash(request.Token);

        // 2. Leer umbral de expiración
        int expiryHours = await GetThresholdHoursAsync("email_verification_expiry_hours", 24, cancellationToken);

        // 3. Buscar usuario con el hash y que no haya expirado
        // La expiración se calcula como: CreatedAt + expiryHours
        // Dado que no tenemos columna expiry separada en User, verificamos contra created_at
        var cutoff = DateTime.UtcNow.AddHours(-expiryHours);

        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.EmailVerificationToken == tokenHash &&
                u.EmailVerifiedAt == null &&
                u.CreatedAt > cutoff,
                cancellationToken);

        if (user is null)
            return Result<AuthTokenResponse>.Failure(
                "El enlace de verificación no es válido o ha expirado.", "INVALID_VERIFICATION_TOKEN");

        // 4. Marcar email como verificado
        user.EmailVerifiedAt          = DateTime.UtcNow;
        user.EmailVerificationToken   = null;
        await _context.SaveChangesAsync(cancellationToken);

        // 5. Generar tokens (primer login automático)
        bool onboardingComplete = user.DisclaimerAcceptedAt.HasValue;

        string accessToken  = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email, user.FullName, emailVerified: true, onboardingComplete, user.Role);
        string refreshToken = _jwtTokenService.GenerateRefreshToken();

        await SaveRefreshTokenAsync(user.Id, refreshToken, cancellationToken);

        int accessTokenExpiry = await GetThresholdMinutesAsync("access_token_expiry_minutes", 15, cancellationToken);

        return Result<AuthTokenResponse>.Success(new AuthTokenResponse
        {
            AccessToken  = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn    = accessTokenExpiry * 60,
            User = new UserSummaryDto
            {
                UserId            = user.Id,
                FullName          = user.FullName,
                Email             = user.Email,
                EmailVerified     = true,
                OnboardingComplete = onboardingComplete,
            },
        });
    }

    private async Task SaveRefreshTokenAsync(Guid userId, string rawToken, CancellationToken ct)
    {
        int expiryDays = await GetThresholdDaysAsync("refresh_token_expiry_days", 30, ct);
        string tokenHash = RegisterUserCommandHandler.ComputeSha256Hash(rawToken);

        var refreshToken = new NutriCasa.Domain.Entities.RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            CreatedAt = DateTime.UtcNow,
            UserAgent = _currentUserService.UserAgent,
            IpAddress = System.Net.IPAddress.TryParse(_currentUserService.IpAddress, out var ip) ? ip : null,
        };
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(ct);
    }

    private async Task<int> GetThresholdHoursAsync(string code, int def, CancellationToken ct)
    {
        var t = await _context.SystemThresholds.FirstOrDefaultAsync(x => x.Code == code && x.IsActive, ct);
        return (int)(t?.NumericValue ?? def);
    }

    private async Task<int> GetThresholdMinutesAsync(string code, int def, CancellationToken ct)
    {
        var t = await _context.SystemThresholds.FirstOrDefaultAsync(x => x.Code == code && x.IsActive, ct);
        return (int)(t?.NumericValue ?? def);
    }

    private async Task<int> GetThresholdDaysAsync(string code, int def, CancellationToken ct)
    {
        var t = await _context.SystemThresholds.FirstOrDefaultAsync(x => x.Code == code && x.IsActive, ct);
        return (int)(t?.NumericValue ?? def);
    }
}
