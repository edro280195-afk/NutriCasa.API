using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Challenges.Services;
using ChallengeDto = NutriCasa.Application.Features.Challenges.Queries.GetActiveChallenges.ChallengeDto;

namespace NutriCasa.Application.Features.Challenges.Queries.GetMyChallenges;

public record GetMyChallengesQuery : IRequest<Result<List<ChallengeDto>>>;

public class GetMyChallengesQueryHandler : IRequestHandler<GetMyChallengesQuery, Result<List<ChallengeDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyChallengesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<ChallengeDto>>> Handle(GetMyChallengesQuery request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<List<ChallengeDto>>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var challenges = await _context.ChallengeParticipants
            .Include(p => p.Challenge).ThenInclude(c => c.CreatedByUser)
            .Include(p => p.Challenge).ThenInclude(c => c.Participants)
            .Where(p => p.UserId == userId)
            .Select(p => p.Challenge)
            .OrderByDescending(c => c.CreatedAt)
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
                HasJoined = true,
                CreatedBy = c.CreatedByUser?.FullName ?? "Alguien",
                MyCurrentScore = score,
            };
        }).ToList();

        return Result<List<ChallengeDto>>.Success(result);
    }
}
