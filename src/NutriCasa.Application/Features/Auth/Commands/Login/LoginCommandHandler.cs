using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Auth.Commands.RegisterUser;
using NutriCasa.Application.Features.Auth.DTOs;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthTokenResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUserService;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AuthTokenResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar usuario por email (CITEXT: comparación case-insensitive en BD)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        // Respuesta idéntica si el usuario no existe (evitar enumeración de emails)
        if (user is null)
        {
            await Task.Delay(300, cancellationToken); // timing attack mitigation
            return Result<AuthTokenResponse>.Failure(
                "Email o contraseña incorrectos.", "INVALID_CREDENTIALS");
        }

        // 2. Verificar bloqueo de cuenta
        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
        {
            int minutesRemaining = (int)Math.Ceiling(
                (user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
            return Result<AuthTokenResponse>.Failure(
                $"Cuenta bloqueada. Intenta en {minutesRemaining} minuto(s).",
                "ACCOUNT_LOCKED");
        }

        // 3. Verificar contraseña
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            await HandleFailedLoginAsync(user, cancellationToken);
            return Result<AuthTokenResponse>.Failure(
                "Email o contraseña incorrectos.", "INVALID_CREDENTIALS");
        }

        // 4. Login exitoso — resetear contadores
        user.FailedLoginAttempts = 0;
        user.LockedUntil         = null;
        user.LastLoginAt         = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // 5. Generar tokens
        bool emailVerified     = user.EmailVerifiedAt.HasValue;
        bool onboardingComplete = user.DisclaimerAcceptedAt.HasValue;

        string accessToken  = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email, user.FullName, emailVerified, onboardingComplete);
        string rawRefresh   = _jwtTokenService.GenerateRefreshToken();

        await SaveRefreshTokenAsync(user.Id, rawRefresh, cancellationToken);

        int accessTokenExpiryMinutes = await GetThresholdAsync("access_token_expiry_minutes", 15, cancellationToken);

        return Result<AuthTokenResponse>.Success(new AuthTokenResponse
        {
            AccessToken  = accessToken,
            RefreshToken = rawRefresh,
            ExpiresIn    = accessTokenExpiryMinutes * 60,
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

    private async Task HandleFailedLoginAsync(User user, CancellationToken ct)
    {
        user.FailedLoginAttempts++;

        int lockThreshold1 = await GetThresholdAsync("login_lock_threshold_attempts", 5, ct);
        int lockMinutes    = await GetThresholdAsync("login_lock_duration_minutes", 15, ct);
        int lockThreshold2 = await GetThresholdAsync("login_hard_lock_threshold_attempts", 10, ct);

        if (user.FailedLoginAttempts >= lockThreshold2)
        {
            // Bloqueo fuerte: hasta reset por email
            user.LockedUntil = DateTime.UtcNow.AddDays(365);
        }
        else if (user.FailedLoginAttempts >= lockThreshold1)
        {
            user.LockedUntil = DateTime.UtcNow.AddMinutes(lockMinutes);

            // Notificar por email
            try
            {
                await _emailService.SendLoginAlertAsync(
                    user.Email, user.FullName,
                    _currentUserService.IpAddress ?? "desconocida",
                    DateTime.UtcNow, ct);
            }
            catch { /* no bloquear el flujo si el email falla */ }
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task SaveRefreshTokenAsync(Guid userId, string rawToken, CancellationToken ct)
    {
        int expiryDays = await GetThresholdAsync("refresh_token_expiry_days", 30, ct);
        string tokenHash = RegisterUserCommandHandler.ComputeSha256Hash(rawToken);

        var token = new NutriCasa.Domain.Entities.RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            CreatedAt = DateTime.UtcNow,
            UserAgent = _currentUserService.UserAgent,
            IpAddress = System.Net.IPAddress.TryParse(_currentUserService.IpAddress, out var ip) ? ip : null,
        };
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync(ct);
    }

    private async Task<int> GetThresholdAsync(string code, int def, CancellationToken ct)
    {
        var t = await _context.SystemThresholds
            .FirstOrDefaultAsync(x => x.Code == code && x.IsActive, ct);
        return (int)(t?.NumericValue ?? def);
    }
}
