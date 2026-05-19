using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Challenges.Commands.LeaveChallenge;

public record LeaveChallengeCommand : IRequest<Result>
{
    public required Guid ChallengeId { get; init; }
}

public class LeaveChallengeCommandHandler : IRequestHandler<LeaveChallengeCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public LeaveChallengeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(LeaveChallengeCommand request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var participant = await _context.ChallengeParticipants
            .FirstOrDefaultAsync(p => p.ChallengeId == request.ChallengeId && p.UserId == userId, ct);

        if (participant is null)
            return Result.Failure("No estás inscrito en este reto.", "NOT_JOINED");

        _context.ChallengeParticipants.Remove(participant);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
