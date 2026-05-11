using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Meals.Commands.LogMeal;

public record LogMealCommand : IRequest<Result>
{
    public required Guid PlanMealId { get; init; }
    public required string Status { get; init; }
    public string? SubstitutionNote { get; init; }
    public decimal? ActualPortion { get; init; }
}

public class LogMealCommandHandler : IRequestHandler<LogMealCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public LogMealCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result> Handle(LogMealCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var planMeal = await _context.WeeklyPlanMeals
            .Include(m => m.Plan)
            .Include(m => m.Recipe)
            .FirstOrDefaultAsync(m => m.Id == request.PlanMealId, cancellationToken);

        if (planMeal is null)
            return Result.Failure("Comida no encontrada en el plan.", "NOT_FOUND");

        if (planMeal.Plan.UserId != userId)
            return Result.Failure("Esta comida no pertenece a tu plan.", "FORBIDDEN");

        if (!Enum.TryParse<MealLogStatus>(request.Status, ignoreCase: true, out var status))
            return Result.Failure("Estado inválido. Usa: completed, partial, skipped, substituted.", "INVALID_STATUS");

        if (status == MealLogStatus.Substituted && string.IsNullOrWhiteSpace(request.SubstitutionNote))
            return Result.Failure("Debes proporcionar una nota de sustitución.", "SUBSTITUTION_NOTE_REQUIRED");

        var today = DateOnly.FromDateTime(_dateTimeService.UtcNow);

        // Verificar si ya hay un log para este planMeal hoy
        var existingLog = await _context.MealLogs
            .FirstOrDefaultAsync(l => l.PlanMealId == request.PlanMealId
                                   && l.LoggedForDate == today, cancellationToken);

        if (existingLog is not null)
        {
            // Update existing log
            existingLog.Status = status;
            existingLog.SubstitutionNote = request.SubstitutionNote;
            existingLog.ActualPortion = request.ActualPortion;
            existingLog.LoggedAt = _dateTimeService.UtcNow;
        }
        else
        {
            var log = new MealLog
            {
                UserId = userId,
                PlanMealId = request.PlanMealId,
                RecipeId = planMeal.RecipeId,
                Status = status,
                SubstitutionNote = request.SubstitutionNote,
                ActualPortion = request.ActualPortion,
                LoggedForDate = today,
                LoggedAt = _dateTimeService.UtcNow,
            };
            _context.MealLogs.Add(log);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
