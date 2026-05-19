using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Family.Commands.ChangeMemberRole;

public record ChangeMemberRoleCommand : IRequest<Result>
{
    public required Guid TargetUserId { get; init; }
    public required string NewRole { get; init; }
}

public class ChangeMemberRoleCommandHandler : IRequestHandler<ChangeMemberRoleCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ChangeMemberRoleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(ChangeMemberRoleCommand request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        if (!Enum.TryParse<GroupRole>(request.NewRole, true, out var newRole))
            return Result.Failure("Rol no válido. Usa: owner, admin, member.", "INVALID_ROLE");

        if (newRole == GroupRole.Owner)
            return Result.Failure("No puedes asignar owner directamente. Usa transferir propiedad.", "USE_TRANSFER");

        if (newRole == GroupRole.Pending)
            return Result.Failure("No puedes asignar el rol pending manualmente.", "INVALID_ROLE");

        var userId = _currentUserService.UserId.Value;

        var currentMembership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (currentMembership is null)
            return Result.Failure("No perteneces a ningún grupo.", "NO_GROUP");

        if (currentMembership.Role != GroupRole.Owner && currentMembership.Role != GroupRole.Admin)
            return Result.Failure("Solo owner o admin pueden cambiar roles.", "FORBIDDEN");

        if (request.TargetUserId == userId)
            return Result.Failure("No puedes cambiar tu propio rol.", "SELF_ROLE");

        var targetMembership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == currentMembership.GroupId
                                   && m.UserId == request.TargetUserId
                                   && m.LeftAt == null, ct);

        if (targetMembership is null)
            return Result.Failure("El usuario no es miembro del grupo.", "TARGET_NOT_MEMBER");

        if (targetMembership.Role == GroupRole.Owner)
            return Result.Failure("No puedes cambiar el rol del owner.", "CANNOT_CHANGE_OWNER");

        targetMembership.Role = newRole;

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
