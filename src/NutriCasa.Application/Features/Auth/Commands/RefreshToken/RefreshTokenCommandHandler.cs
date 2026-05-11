using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Auth.Commands.RegisterUser;
using NutriCasa.Application.Features.Auth.DTOs;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokenResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUserService _currentUserService;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AuthTokenResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        string tokenHash = RegisterUserCommandHandler.ComputeSha256Hash(request.RefreshToken);

        // 1. Buscar refresh token válido
        var existingToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt =>
                rt.TokenHash == tokenHash &&
                rt.RevokedAt == null &&
                rt.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        if (existingToken is null)
            return Result<AuthTokenResponse>.Failure(
                "Token de actualización inválido o expirado.", "INVALID_REFRESH_TOKEN");

        // 2. Revocar el token actual (rotación)
        existingToken.RevokedAt = DateTime.UtcNow;

        // 3. Generar nuevo par
        var user = existingToken.User;
        bool emailVerified     = user.EmailVerifiedAt.HasValue;
        bool onboardingComplete = user.DisclaimerAcceptedAt.HasValue;

        string newAccessToken = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email, user.FullName, emailVerified, onboardingComplete, user.Role);
        string newRawRefresh  = _jwtTokenService.GenerateRefreshToken();

        int expiryDays = await GetThresholdAsync("refresh_token_expiry_days", 30, cancellationToken);
        string newTokenHash = RegisterUserCommandHandler.ComputeSha256Hash(newRawRefresh);

        var newToken = new Domain.Entities.RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            TokenHash = newTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            CreatedAt = DateTime.UtcNow,
            UserAgent = _currentUserService.UserAgent,
            IpAddress = System.Net.IPAddress.TryParse(_currentUserService.IpAddress, out var ip) ? ip : null,
        };
        _context.RefreshTokens.Add(newToken);
        await _context.SaveChangesAsync(cancellationToken);

        int accessTokenMinutes = await GetThresholdAsync("access_token_expiry_minutes", 15, cancellationToken);

        return Result<AuthTokenResponse>.Success(new AuthTokenResponse
        {
            AccessToken  = newAccessToken,
            RefreshToken = newRawRefresh,
            ExpiresIn    = accessTokenMinutes * 60,
            User = new UserSummaryDto
            {
                UserId             = user.Id,
                FullName           = user.FullName,
                Email              = user.Email,
                EmailVerified      = emailVerified,
                OnboardingComplete = onboardingComplete,
            },
        });
    }

    private async Task<int> GetThresholdAsync(string code, int def, CancellationToken ct)
    {
        var t = await _context.SystemThresholds
            .FirstOrDefaultAsync(x => x.Code == code && x.IsActive, ct);
        return (int)(t?.NumericValue ?? def);
    }
}
