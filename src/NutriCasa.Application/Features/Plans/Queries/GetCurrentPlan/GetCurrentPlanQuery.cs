using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Plans.Commands.GeneratePlan;

namespace NutriCasa.Application.Features.Plans.Queries.GetCurrentPlan;

public record GetCurrentPlanQuery : IRequest<Result<PlanGenerationResult>>;

public class GetCurrentPlanQueryHandler : IRequestHandler<GetCurrentPlanQuery, Result<PlanGenerationResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentPlanQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PlanGenerationResult>> Handle(GetCurrentPlanQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result<PlanGenerationResult>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var plan = await _context.WeeklyPlans
            .Include(p => p.Meals).ThenInclude(m => m.Recipe)
            .Include(p => p.BudgetMode)
            .Include(p => p.User).ThenInclude(u => u.KetoProfile)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, cancellationToken);

        if (plan is null)
            return Result<PlanGenerationResult>.Failure("No hay un plan activo.", "NO_ACTIVE_PLAN");

        var meals = plan.Meals
            .OrderBy(m => m.DayOfWeek).ThenBy(m => m.SortOrder)
            .ToList();

        var days = meals
            .GroupBy(m => m.DayOfWeek)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var dayMeals = g.OrderBy(m => m.SortOrder).ToList();
                return new DayPlanDto
                {
                    DayNumber = g.Key,
                    DayName = g.Key switch
                    {
                        1 => "Lunes", 2 => "Martes", 3 => "Miércoles", 4 => "Jueves",
                        5 => "Viernes", 6 => "Sábado", 7 => "Domingo", _ => "Día"
                    },
                    Meals = dayMeals.Select(m => new MealPlanDto
                    {
                        PlanMealId = m.Id,
                        MealType = m.MealType.ToString().ToLowerInvariant(),
                        IsLocked = m.IsLocked,
                        PortionMultiplier = m.PortionMultiplier,
                        RowVersion = m.RowVersion,
                        Recipe = new RecipeDto
                        {
                            RecipeId = m.Recipe!.Id,
                            Name = m.Recipe.Name,
                            Calories = m.Recipe.BaseCalories,
                            ProteinGr = m.Recipe.BaseProteinGr,
                            FatGr = m.Recipe.BaseFatGr,
                            CarbsGr = m.Recipe.BaseCarbsGr,
                            PrepTimeMin = m.Recipe.PrepTimeMin ?? 0,
                            CookTimeMin = m.Recipe.CookTimeMin ?? 0,
                            Instructions = m.Recipe.Instructions ?? "",
                            EstimatedCostMxn = m.Recipe.EstimatedCostPerServingMxn ?? 0,
                            PrimaryStore = null,
                            Ingredients = RecipeIngredientParser.Parse(m.Recipe.Ingredients),
                        },
                    }).ToList(),
                    DayTotals = new DayTotalsDto
                    {
                        Calories = dayMeals.Sum(m => m.Recipe?.BaseCalories ?? 0),
                        ProteinGr = dayMeals.Sum(m => m.Recipe?.BaseProteinGr ?? 0),
                        FatGr = dayMeals.Sum(m => m.Recipe?.BaseFatGr ?? 0),
                        CarbsGr = dayMeals.Sum(m => m.Recipe?.BaseCarbsGr ?? 0),
                        EstimatedCostMxn = dayMeals.Sum(m => m.Recipe?.EstimatedCostPerServingMxn ?? 0),
                    },
                };
            }).ToList();

        var catalogRaw = await _context.IngredientCatalog
            .Where(c => c.IsActive && c.PrimaryStoreCategory != null)
            .Select(c => new { c.Name, c.PrimaryStoreCategory })
            .ToListAsync(cancellationToken);
        var catalog = catalogRaw
            .Select(c => (c.Name.ToLowerInvariant(), c.PrimaryStoreCategory!))
            .ToList();

        var kp = plan.User?.KetoProfile;
        var result = new PlanGenerationResult
        {
            PlanId = plan.Id,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            BudgetModeCode = plan.BudgetMode?.Code ?? "unknown",
            BudgetModeName = plan.BudgetMode?.Name ?? "Desconocido",
            IsOverridePlan = plan.IsOverridePlan,
            EstimatedCostMxn = plan.EstimatedTotalCostMxn,
            SavingsVsGourmetMxn = plan.SavingsVsGourmetMxn,
            SavingsVsGourmetPercent = plan.SavingsVsGourmetPercent,
            Days = days,
            Macros = kp is not null ? new KetoProfileResult
            {
                BmrKcal = kp.BmrKcal ?? 0,
                TdeeKcal = kp.TdeeKcal ?? 0,
                DailyCalories = kp.DailyCalories,
                CarbsGrams = kp.CarbsGrams,
                ProteinGrams = kp.ProteinGrams,
                FatGrams = kp.FatGrams,
                CarbsPercent = kp.CarbsPercent ?? 0,
                ProteinPercent = kp.ProteinPercent ?? 0,
                FatPercent = kp.FatPercent ?? 0,
            } : new KetoProfileResult(),
            ShoppingList = GeneratePlanCommandHandler.BuildShoppingList(plan.Meals, catalog),
        };

        return Result<PlanGenerationResult>.Success(result);
    }
}
