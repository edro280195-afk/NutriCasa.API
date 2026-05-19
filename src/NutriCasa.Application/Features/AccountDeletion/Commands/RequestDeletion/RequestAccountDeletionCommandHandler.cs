using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Constants;

namespace NutriCasa.Application.Features.AccountDeletion.Commands.RequestDeletion;

public class RequestAccountDeletionCommandHandler : IRequestHandler<RequestAccountDeletionCommand, Result<DeletionScheduledResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RequestAccountDeletionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<DeletionScheduledResponse>> Handle(RequestAccountDeletionCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<DeletionScheduledResponse>.Failure("No autenticado.", "UNAUTHORIZED");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, ct);

        if (user is null)
            return Result<DeletionScheduledResponse>.Failure("Usuario no encontrado.", "NOT_FOUND");

        if (user.DeletionRequestedAt is not null && user.DeletionCancelledAt is null)
            return Result<DeletionScheduledResponse>.Failure("Ya tienes una solicitud de borrado activa.", "DELETION_ALREADY_REQUESTED");

        var threshold = await _context.SystemThresholds
            .FirstOrDefaultAsync(t => t.Code == SystemThresholdCodes.AccountDeletionGraceDays, ct);

        int graceDays = (int?)threshold?.NumericValue ?? 30;

        var now = DateTime.UtcNow;
        user.DeletionRequestedAt = now;
        user.DeletionScheduledFor = now.AddDays(graceDays);
        user.DeletionCancelledAt = null;

        await _context.SaveChangesAsync(ct);

        return Result<DeletionScheduledResponse>.Success(new DeletionScheduledResponse
        {
            DeletionRequestedAt = now,
            DeletionScheduledFor = now.AddDays(graceDays),
            GraceDays = graceDays,
        });
    }
}
