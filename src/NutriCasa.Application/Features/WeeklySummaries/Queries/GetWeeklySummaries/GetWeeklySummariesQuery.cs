using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.WeeklySummaries.Queries.GetWeeklySummaries;

public record GetWeeklySummariesQuery : IRequest<Result<List<WeeklySummaryDto>>>;

public record WeeklySummaryDto
{
    public Guid SummaryId { get; init; }
    public DateOnly WeekStartDate { get; init; }
    public DateOnly WeekEndDate { get; init; }
    public decimal? AvgDifficulty { get; init; }
    public decimal? AvgHunger { get; init; }
    public decimal? AvgEnergy { get; init; }
    public decimal? AdherencePercent { get; init; }
    public decimal? WeightChangeKg { get; init; }
    public int? CheckInsCount { get; init; }
    public bool IsInPlateau { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class GetWeeklySummariesQueryHandler : IRequestHandler<GetWeeklySummariesQuery, Result<List<WeeklySummaryDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetWeeklySummariesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<WeeklySummaryDto>>> Handle(GetWeeklySummariesQuery request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<List<WeeklySummaryDto>>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var summaries = await _context.WeeklySummaries
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.WeekStartDate)
            .Select(s => new WeeklySummaryDto
            {
                SummaryId = s.Id,
                WeekStartDate = s.WeekStartDate,
                WeekEndDate = s.WeekEndDate,
                AvgDifficulty = s.AvgDifficulty,
                AvgHunger = s.AvgHunger,
                AvgEnergy = s.AvgEnergy,
                AdherencePercent = s.AdherencePercent,
                WeightChangeKg = s.WeightChangeKg,
                CheckInsCount = s.CheckInsCount,
                IsInPlateau = s.IsInPlateau,
                CreatedAt = s.CreatedAt,
            })
            .ToListAsync(ct);

        return Result<List<WeeklySummaryDto>>.Success(summaries);
    }
}
