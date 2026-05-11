using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Meals.DTOs;

namespace NutriCasa.Application.Features.Meals.Queries.GetMealLogs;

public record GetMealLogsQuery : IRequest<Result<List<MealLogDto>>>
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
}

public class GetMealLogsQueryHandler : IRequestHandler<GetMealLogsQuery, Result<List<MealLogDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMealLogsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<MealLogDto>>> Handle(GetMealLogsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result<List<MealLogDto>>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var query = _context.MealLogs
            .Include(l => l.PlanMeal)
            .ThenInclude(m => m!.Recipe)
            .Where(l => l.UserId == userId);

        if (request.DateFrom.HasValue)
            query = query.Where(l => l.LoggedForDate >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(l => l.LoggedForDate <= request.DateTo.Value);

        var logs = await query
            .OrderByDescending(l => l.LoggedForDate)
            .ThenBy(l => l.LoggedAt)
            .Select(l => new MealLogDto
            {
                LogId = l.Id,
                PlanMealId = l.PlanMealId,
                Status = l.Status.ToString(),
                SubstitutionNote = l.SubstitutionNote,
                ActualPortion = l.ActualPortion,
                RecipeName = l.PlanMeal!.Recipe!.Name,
                MealType = l.PlanMeal.MealType.ToString(),
                LoggedForDate = l.LoggedForDate,
                LoggedAt = l.LoggedAt,
            })
            .ToListAsync(cancellationToken);

        return Result<List<MealLogDto>>.Success(logs);
    }
}
