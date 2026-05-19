using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.Challenges.Commands.JoinChallenge;

public record JoinChallengeCommand : IRequest<Result>
{
    public required Guid ChallengeId { get; init; }
}

public class JoinChallengeCommandHandler : IRequestHandler<JoinChallengeCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public JoinChallengeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(JoinChallengeCommand request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var challenge = await _context.Challenges
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId, ct);

        if (challenge is null)
            return Result.Failure("Reto no encontrado.", "NOT_FOUND");

        if (!challenge.IsActive || challenge.IsFinalized)
            return Result.Failure("Este reto ya no está activo.", "CHALLENGE_CLOSED");

        if (challenge.EndDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return Result.Failure("Este reto ya terminó.", "CHALLENGE_ENDED");

        var membership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (membership is null || membership.GroupId != challenge.GroupId)
            return Result.Failure("No eres miembro del grupo de este reto.", "NO_GROUP");

        var alreadyJoined = await _context.ChallengeParticipants
            .AnyAsync(p => p.ChallengeId == request.ChallengeId && p.UserId == userId, ct);

        if (alreadyJoined)
            return Result.Failure("Ya estás inscrito en este reto.", "ALREADY_JOINED");

        var participant = new ChallengeParticipant
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            StartingValue = await GetStartingValueAsync(userId, challenge.GoalType, ct),
        };

        _context.ChallengeParticipants.Add(participant);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    private async Task<decimal?> GetStartingValueAsync(Guid userId, Domain.Enums.ChallengeGoalType goalType, CancellationToken ct)
    {
        if (goalType is Domain.Enums.ChallengeGoalType.MostWeightLoss or Domain.Enums.ChallengeGoalType.MostFatLoss)
        {
            var first = await _context.BodyMeasurements
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.MeasuredAt)
                .Select(m => (decimal?)m.WeightKg)
                .FirstOrDefaultAsync(ct);

            return first;
        }

        return null;
    }
}
