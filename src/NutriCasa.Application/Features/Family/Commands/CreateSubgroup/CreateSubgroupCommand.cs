using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Family.Commands.CreateSubgroup;

public record CreateSubgroupCommand : IRequest<Result<SubgroupCreatedDto>>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public class SubgroupCreatedDto
{
    public Guid SubgroupId { get; set; }
    public string SubgroupName { get; set; } = "";
    public string InviteCode { get; set; } = "";
}

public class CreateSubgroupCommandHandler : IRequestHandler<CreateSubgroupCommand, Result<SubgroupCreatedDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateSubgroupCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<SubgroupCreatedDto>> Handle(CreateSubgroupCommand request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<SubgroupCreatedDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var membership = await _context.GroupMemberships
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (membership is null)
            return Result<SubgroupCreatedDto>.Failure("No perteneces a ningún grupo.", "NO_GROUP");

        if (membership.Role != GroupRole.Owner && membership.Role != GroupRole.Admin)
            return Result<SubgroupCreatedDto>.Failure("Solo el owner o admin pueden crear subgrupos.", "FORBIDDEN");

        var parentGroup = membership.Group;

        // Contar subgrupos existentes para validar profundidad máxima (3 niveles)
        var depth = await GetGroupDepthAsync(parentGroup.Id, ct);
        if (depth >= 2)
            return Result<SubgroupCreatedDto>.Failure("Ya alcanzaste el máximo de 3 niveles de subgrupos.", "MAX_DEPTH");

        var subgroup = new Group
        {
            Id = Guid.NewGuid(),
            ParentGroupId = parentGroup.Id,
            Name = request.Name,
            Description = request.Description,
            InviteCode = GenerateInviteCode(),
            GroupType = GroupType.Subgroup,
            CreatedByUserId = userId,
        };

        var subgroupMembership = new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = subgroup.Id,
            UserId = userId,
            Role = GroupRole.Owner,
            JoinedAt = DateTime.UtcNow,
        };

        membership.LeftAt = DateTime.UtcNow;

        _context.Groups.Add(subgroup);
        _context.GroupMemberships.Add(subgroupMembership);
        await _context.SaveChangesAsync(ct);

        return Result<SubgroupCreatedDto>.Success(new SubgroupCreatedDto
        {
            SubgroupId = subgroup.Id,
            SubgroupName = subgroup.Name,
            InviteCode = subgroup.InviteCode,
        });
    }

    private async Task<int> GetGroupDepthAsync(Guid groupId, CancellationToken ct)
    {
        int depth = 0;
        var currentId = groupId;

        while (true)
        {
            var parentId = await _context.Groups
                .Where(g => g.Id == currentId)
                .Select(g => g.ParentGroupId)
                .FirstOrDefaultAsync(ct);

            if (parentId is null) break;
            currentId = parentId.Value;
            depth++;
        }

        return depth;
    }

    private static string GenerateInviteCode()
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var letters = new string(Enumerable.Repeat(chars, 4).Select(s => s[random.Next(s.Length)]).ToArray());
        var numbers = random.Next(1000, 10000).ToString();
        return $"NUT-{letters}-{numbers}";
    }
}
