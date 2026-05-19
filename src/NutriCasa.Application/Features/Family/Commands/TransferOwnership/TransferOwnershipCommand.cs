using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Family.Commands.TransferOwnership;

public record TransferOwnershipCommand : IRequest<Result>
{
    public required Guid TargetUserId { get; init; }
}

public class TransferOwnershipCommandHandler : IRequestHandler<TransferOwnershipCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public TransferOwnershipCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(TransferOwnershipCommand request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var currentMembership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (currentMembership is null)
            return Result.Failure("No perteneces a ningún grupo.", "NO_GROUP");

        if (currentMembership.Role != GroupRole.Owner)
            return Result.Failure("Solo el owner puede transferir la propiedad.", "FORBIDDEN");

        if (request.TargetUserId == userId)
            return Result.Failure("No puedes transferirte la propiedad a ti mismo.", "SAME_USER");

        var targetMembership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == currentMembership.GroupId
                                   && m.UserId == request.TargetUserId
                                   && m.LeftAt == null, ct);

        if (targetMembership is null)
            return Result.Failure("El usuario destino no es miembro del grupo.", "TARGET_NOT_MEMBER");

        currentMembership.Role = GroupRole.Admin;
        targetMembership.Role = GroupRole.Owner;

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
