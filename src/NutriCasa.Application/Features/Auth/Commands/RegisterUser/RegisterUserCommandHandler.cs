using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Auth.DTOs;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Auth.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<AuthTokenResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUserService _currentUserService;

    public RegisterUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _jwtTokenService = jwtTokenService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AuthTokenResponse>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (emailExists)
            return Result<AuthTokenResponse>.Failure(
                "Ya existe una cuenta con este email.", "EMAIL_TAKEN");

        var groupToJoinResult = await GetGroupToJoinAsync(request.GroupCode, cancellationToken);
        if (!groupToJoinResult.IsSuccess)
            return Result<AuthTokenResponse>.Failure(groupToJoinResult.Error!, groupToJoinResult.ErrorCode);

        string rawToken = Guid.NewGuid().ToString("N");
        string tokenHash = ComputeSha256Hash(rawToken);
        string passwordHash = _passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            BirthDate = request.BirthDate,
            EmailVerificationToken = tokenHash,
            EmailVerifiedAt = null,
        };

        var privacySettings = new PrivacySettings
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.Users.Add(user);
        _context.PrivacySettings.Add(privacySettings);

        if (groupToJoinResult.Value is not null)
        {
            _context.GroupMemberships.Add(new GroupMembership
            {
                Id = Guid.NewGuid(),
                GroupId = groupToJoinResult.Value.Id,
                UserId = user.Id,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        string verificationLink = $"https://nutricasa.app/auth/verify-email?token={rawToken}";
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

        string refreshToken = _jwtTokenService.GenerateRefreshToken();
        await SaveRefreshTokenAsync(user.Id, refreshToken, cancellationToken);

        bool emailVerified = user.EmailVerifiedAt.HasValue;
        bool onboardingComplete = user.DisclaimerAcceptedAt.HasValue;
        int accessTokenExpiryMinutes = await GetThresholdAsync("access_token_expiry_minutes", 15, cancellationToken);
        string accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email, user.FullName, emailVerified, onboardingComplete, user.Role);

        return Result<AuthTokenResponse>.Success(new AuthTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = accessTokenExpiryMinutes * 60,
            User = new UserSummaryDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                EmailVerified = emailVerified,
                OnboardingComplete = onboardingComplete,
            },
        });
    }

    private async Task<Result<Group?>> GetGroupToJoinAsync(string? groupCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(groupCode))
            return Result<Group?>.Success(null);

        var inviteCode = groupCode.Trim().ToUpperInvariant();
        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.InviteCode == inviteCode, cancellationToken);

        if (group is null)
            return Result<Group?>.Failure("El codigo de grupo no es valido.", "INVALID_GROUP_CODE");

        if (group.IsArchived || group.IsFrozen)
            return Result<Group?>.Failure("Este grupo no esta disponible.", "GROUP_UNAVAILABLE");

        if (group.InviteCodeExpiresAt.HasValue && group.InviteCodeExpiresAt.Value < DateTime.UtcNow)
            return Result<Group?>.Failure("El codigo de invitacion ha expirado.", "CODE_EXPIRED");

        var currentMembers = await _context.GroupMemberships
            .CountAsync(m => m.GroupId == group.Id && m.LeftAt == null, cancellationToken);

        var maxGroupMembers = await GetMaxGroupMembersAsync(group, cancellationToken);
        if (maxGroupMembers.HasValue && currentMembers >= maxGroupMembers.Value)
            return Result<Group?>.Failure(
                $"Este grupo ya llego al limite de {maxGroupMembers.Value} miembros de su plan.",
                "GROUP_MEMBER_LIMIT_REACHED");

        return Result<Group?>.Success(group);
    }

    private async Task SaveRefreshTokenAsync(Guid userId, string rawToken, CancellationToken ct)
    {
        int expiryDays = await GetThresholdAsync("refresh_token_expiry_days", 30, ct);
        string tokenHash = ComputeSha256Hash(rawToken);

        _context.RefreshTokens.Add(new NutriCasa.Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            CreatedAt = DateTime.UtcNow,
            UserAgent = _currentUserService.UserAgent,
            IpAddress = System.Net.IPAddress.TryParse(_currentUserService.IpAddress, out var ip) ? ip : null,
        });

        await _context.SaveChangesAsync(ct);
    }

    private async Task<int?> GetMaxGroupMembersAsync(Group group, CancellationToken cancellationToken)
    {
        var ownerUserId = group.CreatedByUserId;

        if (ownerUserId is null)
        {
            ownerUserId = await _context.GroupMemberships
                .Where(m => m.GroupId == group.Id && m.Role == GroupRole.Owner && m.LeftAt == null)
                .Select(m => (Guid?)m.UserId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (ownerUserId is not null)
        {
            var ownerPlanLimit = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == ownerUserId.Value
                         && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing))
                .OrderByDescending(s => s.StartedAt)
                .Select(s => s.Plan.MaxGroupMembers)
                .FirstOrDefaultAsync(cancellationToken);

            if (ownerPlanLimit is not null)
                return ownerPlanLimit;

            var hasUnlimitedOwnerPlan = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .AnyAsync(s => s.UserId == ownerUserId.Value
                            && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing)
                            && s.Plan.MaxGroupMembers == null, cancellationToken);

            if (hasUnlimitedOwnerPlan)
                return null;
        }

        return await _context.SubscriptionPlans
            .Where(p => p.Code == "free" && p.IsActive)
            .Select(p => p.MaxGroupMembers)
            .FirstOrDefaultAsync(cancellationToken) ?? 5;
    }

    private async Task<int> GetThresholdAsync(string code, int defaultValue, CancellationToken ct)
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
