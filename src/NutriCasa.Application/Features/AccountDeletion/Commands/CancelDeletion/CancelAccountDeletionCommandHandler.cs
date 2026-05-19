using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.AccountDeletion.Commands.CancelDeletion;

public class CancelAccountDeletionCommandHandler : IRequestHandler<CancelAccountDeletionCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CancelAccountDeletionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CancelAccountDeletionCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, ct);

        if (user is null)
            return Result.Failure("Usuario no encontrado.", "NOT_FOUND");

        if (user.DeletionRequestedAt is null)
            return Result.Failure("No hay una solicitud de borrado activa.", "NO_DELETION_REQUEST");

        user.DeletionCancelledAt = DateTime.UtcNow;
        user.DeletionRequestedAt = null;
        user.DeletionScheduledFor = null;

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
