using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Plans.Commands.ReorderMeals;

public record MealMove
{
    public required Guid PlanMealId { get; init; }
    public required int NewDayOfWeek { get; init; }
    public required string NewMealType { get; init; }
    public required long RowVersion { get; init; }
    public int NewSortOrder { get; init; } = 1;
}

public record ReorderMealsCommand : IRequest<Result>
{
    public required Guid PlanId { get; init; }
    public required List<MealMove> Moves { get; init; }
}

public class ReorderMealsCommandHandler : IRequestHandler<ReorderMealsCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ReorderMealsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(ReorderMealsCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var plan = await _context.WeeklyPlans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);

        if (plan is null)
            return Result.Failure("Plan no encontrado.", "NOT_FOUND");

        if (plan.UserId != userId)
            return Result.Failure("No tienes permiso para modificar este plan.", "FORBIDDEN");

        var mealIds = request.Moves.Select(m => m.PlanMealId).ToList();
        var meals = await _context.WeeklyPlanMeals
            .Where(m => m.PlanId == request.PlanId && mealIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        // Verificar optimistic concurrency
        var conflicts = new List<string>();
        foreach (var move in request.Moves)
        {
            var meal = meals.FirstOrDefault(m => m.Id == move.PlanMealId);
            if (meal is null)
            {
                conflicts.Add($"Comida {move.PlanMealId} no encontrada en el plan.");
                continue;
            }

            if (meal.IsLocked)
            {
                conflicts.Add($"La comida '{meal.Id}' está bloqueada.");
                continue;
            }

            if (meal.RowVersion != move.RowVersion)
                conflicts.Add($"Conflicto en comida {meal.Id}: se esperaba row_version {move.RowVersion}, actual es {meal.RowVersion}.");
        }

        if (conflicts.Count > 0)
            return Result.Failure(
                string.Join(" | ", conflicts),
                "CONFLICT");

        // Aplicar cambios
        foreach (var move in request.Moves)
        {
            var meal = meals.First(m => m.Id == move.PlanMealId);
            if (!Enum.TryParse<MealType>(move.NewMealType, ignoreCase: true, out var mealType))
                continue;

            meal.DayOfWeek = move.NewDayOfWeek;
            meal.MealType = mealType;
            meal.SortOrder = move.NewSortOrder;
            meal.RowVersion++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
