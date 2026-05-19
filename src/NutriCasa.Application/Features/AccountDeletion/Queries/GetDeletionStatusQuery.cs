using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.AccountDeletion.Queries;

public record GetDeletionStatusQuery : IRequest<Result<DeletionStatusDto>>;

public record DeletionStatusDto
{
    public bool HasPendingDeletion { get; init; }
    public DateTime? DeletionRequestedAt { get; init; }
    public DateTime? DeletionScheduledFor { get; init; }
    public int? DaysRemaining { get; init; }
}

public class GetDeletionStatusQueryHandler : IRequestHandler<GetDeletionStatusQuery, Result<DeletionStatusDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetDeletionStatusQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<DeletionStatusDto>> Handle(GetDeletionStatusQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<DeletionStatusDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var user = await _context.Users
            .Where(u => u.Id == _currentUser.UserId.Value)
            .Select(u => new
            {
                u.DeletionRequestedAt,
                u.DeletionScheduledFor,
                u.DeletionCancelledAt
            })
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return Result<DeletionStatusDto>.Failure("Usuario no encontrado.", "NOT_FOUND");

        bool hasPending = user.DeletionRequestedAt is not null && user.DeletionCancelledAt is null;

        return Result<DeletionStatusDto>.Success(new DeletionStatusDto
        {
            HasPendingDeletion = hasPending,
            DeletionRequestedAt = user.DeletionRequestedAt,
            DeletionScheduledFor = user.DeletionScheduledFor,
            DaysRemaining = hasPending && user.DeletionScheduledFor is not null
                ? Math.Max(0, (int)(user.DeletionScheduledFor.Value - DateTime.UtcNow).TotalDays)
                : null,
        });
    }
}
