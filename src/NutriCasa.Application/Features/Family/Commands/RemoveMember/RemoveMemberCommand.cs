using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Family.Commands.RemoveMember;

public record RemoveMemberCommand : IRequest<Result>
{
    public required Guid TargetUserId { get; init; }
}

public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveMemberCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var currentMembership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (currentMembership is null)
            return Result.Failure("No perteneces a ningún grupo.", "NO_GROUP");

        // Si el usuario se está removiendo a sí mismo (salir del grupo)
        if (request.TargetUserId == userId)
        {
            if (currentMembership.Role == GroupRole.Owner)
                return Result.Failure("Debes transferir la propiedad antes de salir del grupo.", "TRANSFER_FIRST");

            currentMembership.LeftAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return Result.Success();
        }

        // Si es admin/owner removiendo a otro miembro
        if (currentMembership.Role != GroupRole.Owner && currentMembership.Role != GroupRole.Admin)
            return Result.Failure("Solo owner o admin pueden remover miembros.", "FORBIDDEN");

        var targetMembership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == currentMembership.GroupId
                                   && m.UserId == request.TargetUserId
                                   && m.LeftAt == null, ct);

        if (targetMembership is null)
            return Result.Failure("El usuario no es miembro del grupo.", "TARGET_NOT_MEMBER");

        if (targetMembership.Role == GroupRole.Owner)
            return Result.Failure("No puedes remover al owner del grupo.", "CANNOT_REMOVE_OWNER");

        targetMembership.LeftAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
