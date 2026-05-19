using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Challenges.Services;

namespace NutriCasa.Application.Features.Challenges.Queries.GetActiveChallenges;

public record GetActiveChallengesQuery : IRequest<Result<List<ChallengeDto>>>;

public class GetActiveChallengesQueryHandler : IRequestHandler<GetActiveChallengesQuery, Result<List<ChallengeDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetActiveChallengesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<ChallengeDto>>> Handle(GetActiveChallengesQuery request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<List<ChallengeDto>>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var membership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (membership is null)
            return Result<List<ChallengeDto>>.Success([]);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var challenges = await _context.Challenges
            .Include(c => c.Participants)
            .Include(c => c.CreatedByUser)
            .Where(c => c.GroupId == membership.GroupId
                     && c.IsActive
                     && !c.IsFinalized
                     && c.EndDate >= today)
            .OrderBy(c => c.EndDate)
            .ToListAsync(ct);

        var userChallengeIds = await _context.ChallengeParticipants
            .Where(p => p.UserId == userId)
            .Select(p => p.ChallengeId)
            .ToListAsync(ct);

        var result = challenges.Select(c =>
        {
            var myParticipation = c.Participants.FirstOrDefault(p => p.UserId == userId);
            var score = ChallengeScoringService.CalculateScore(
                c.GoalType,
                myParticipation?.StartingValue,
                myParticipation?.CurrentValue,
                0, 0, 0);

            return new ChallengeDto
            {
                ChallengeId = c.Id,
                Title = c.Title,
                Description = c.Description,
                GoalType = c.GoalType.ToString(),
                GoalDescription = c.GoalDescription,
                RewardDescription = c.RewardDescription,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                ParticipantCount = c.Participants.Count,
                HasJoined = userChallengeIds.Contains(c.Id),
                CreatedBy = c.CreatedByUser?.FullName ?? "Alguien",
                MyCurrentScore = score,
            };
        }).ToList();

        return Result<List<ChallengeDto>>.Success(result);
    }
}
