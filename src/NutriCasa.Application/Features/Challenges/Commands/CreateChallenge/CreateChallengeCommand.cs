using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Challenges.Commands.CreateChallenge;

public record CreateChallengeCommand : IRequest<Result<ChallengeCreatedDto>>
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string GoalType { get; init; }
    public string? GoalDescription { get; init; }
    public string? RewardDescription { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
}

public record ChallengeCreatedDto
{
    public Guid ChallengeId { get; set; }
    public string Title { get; set; } = "";
}

public class CreateChallengeCommandHandler : IRequestHandler<CreateChallengeCommand, Result<ChallengeCreatedDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateChallengeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ChallengeCreatedDto>> Handle(CreateChallengeCommand request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<ChallengeCreatedDto>.Failure("No autenticado.", "UNAUTHORIZED");

        if (!Enum.TryParse<ChallengeGoalType>(request.GoalType, true, out var goalType))
            return Result<ChallengeCreatedDto>.Failure("Tipo de reto no válido.", "INVALID_GOAL_TYPE");

        if (request.StartDate >= request.EndDate)
            return Result<ChallengeCreatedDto>.Failure("La fecha de inicio debe ser anterior a la de fin.", "INVALID_DATES");

        if (request.EndDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return Result<ChallengeCreatedDto>.Failure("El reto no puede terminar en el pasado.", "PAST_DATE");

        var userId = _currentUserService.UserId.Value;

        var membership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (membership is null)
            return Result<ChallengeCreatedDto>.Failure("No perteneces a ningún grupo.", "NO_GROUP");

        if (membership.Role != GroupRole.Owner && membership.Role != GroupRole.Admin)
            return Result<ChallengeCreatedDto>.Failure("Solo owner o admin pueden crear retos.", "FORBIDDEN");

        var challenge = new Challenge
        {
            Id = Guid.NewGuid(),
            GroupId = membership.GroupId,
            Title = request.Title,
            Description = request.Description,
            GoalType = goalType,
            GoalDescription = request.GoalDescription,
            RewardDescription = request.RewardDescription,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedByUserId = userId,
        };

        _context.Challenges.Add(challenge);

        // El creador se une automáticamente
        var participant = new ChallengeParticipant
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            UserId = userId,
            StartingValue = await GetStartingValueAsync(userId, goalType, ct),
            JoinedAt = DateTime.UtcNow,
        };
        _context.ChallengeParticipants.Add(participant);

        await _context.SaveChangesAsync(ct);

        return Result<ChallengeCreatedDto>.Success(new ChallengeCreatedDto
        {
            ChallengeId = challenge.Id,
            Title = challenge.Title,
        });
    }

    private async Task<decimal?> GetStartingValueAsync(Guid userId, ChallengeGoalType goalType, CancellationToken ct)
    {
        if (goalType is ChallengeGoalType.MostWeightLoss or ChallengeGoalType.MostFatLoss)
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
