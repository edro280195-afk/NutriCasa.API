using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Onboarding.DTOs;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep1Group;

public class CompleteStep1GroupCommandHandler : IRequestHandler<CompleteStep1GroupCommand, Result<CompleteStep1GroupResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CompleteStep1GroupCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CompleteStep1GroupResponse>> Handle(
        CompleteStep1GroupCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result<CompleteStep1GroupResponse>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        if (request.Action == "create")
        {
            var inviteCode = GenerateInviteCode();

            var group = new Group
            {
                Id              = Guid.NewGuid(),
                Name            = request.GroupName!,
                InviteCode      = inviteCode,
                GroupType       = GroupType.Household, // default
                CreatedByUserId = userId
            };

            var membership = new GroupMembership
            {
                Id        = Guid.NewGuid(),
                GroupId   = group.Id,
                UserId    = userId,
                Role      = GroupRole.Owner,
                JoinedAt  = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            _context.GroupMemberships.Add(membership);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<CompleteStep1GroupResponse>.Success(new CompleteStep1GroupResponse
            {
                GroupId    = group.Id,
                InviteCode = inviteCode
            });
        }
        else // join
        {
            var inviteCode = request.InviteCode!.Trim().ToUpperInvariant();

            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.InviteCode == inviteCode, cancellationToken);

            if (group is null)
                return Result<CompleteStep1GroupResponse>.Failure("Código de invitación no válido.", "INVALID_CODE");

            if (group.IsArchived || group.IsFrozen)
                return Result<CompleteStep1GroupResponse>.Failure("Este grupo no está disponible.", "GROUP_UNAVAILABLE");

            if (group.InviteCodeExpiresAt.HasValue && group.InviteCodeExpiresAt.Value < DateTime.UtcNow)
                return Result<CompleteStep1GroupResponse>.Failure("El código de invitación ha expirado.", "CODE_EXPIRED");

            var alreadyMember = await _context.GroupMemberships
                .AnyAsync(m => m.GroupId == group.Id && m.UserId == userId, cancellationToken);

            if (alreadyMember)
                return Result<CompleteStep1GroupResponse>.Failure("Ya eres miembro de este grupo.", "ALREADY_MEMBER");

            var currentMembers = await _context.GroupMemberships
                .CountAsync(m => m.GroupId == group.Id && m.LeftAt == null, cancellationToken);

            var maxGroupMembers = await GetMaxGroupMembersAsync(group, cancellationToken);
            if (maxGroupMembers.HasValue && currentMembers >= maxGroupMembers.Value)
                return Result<CompleteStep1GroupResponse>.Failure(
                    $"Este grupo ya llego al limite de {maxGroupMembers.Value} miembros de su plan.",
                    "GROUP_MEMBER_LIMIT_REACHED");

            var membership = new GroupMembership
            {
                Id        = Guid.NewGuid(),
                GroupId   = group.Id,
                UserId    = userId,
                Role      = GroupRole.Member,
                JoinedAt  = DateTime.UtcNow
            };

            _context.GroupMemberships.Add(membership);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<CompleteStep1GroupResponse>.Success(new CompleteStep1GroupResponse
            {
                GroupId   = group.Id,
                GroupName = group.Name
            });
        }
    }

    private static string GenerateInviteCode()
    {
        // NUT-{4 letras}-{4 dígitos}
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var letters = new string(Enumerable.Repeat(chars, 4).Select(s => s[random.Next(s.Length)]).ToArray());
        var numbers = random.Next(1000, 10000).ToString();
        return $"NUT-{letters}-{numbers}";
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
}
