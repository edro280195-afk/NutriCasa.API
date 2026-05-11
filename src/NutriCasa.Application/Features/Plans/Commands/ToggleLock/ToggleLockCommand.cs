using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Plans.Commands.ToggleLock;

public record ToggleLockCommand : IRequest<Result>
{
    public required Guid PlanMealId { get; init; }
    public required bool Locked { get; init; }
}

public class ToggleLockCommandHandler : IRequestHandler<ToggleLockCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ToggleLockCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(ToggleLockCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var meal = await _context.WeeklyPlanMeals
            .Include(m => m.Plan)
            .FirstOrDefaultAsync(m => m.Id == request.PlanMealId, cancellationToken);

        if (meal is null)
            return Result.Failure("Comida no encontrada.", "NOT_FOUND");

        if (meal.Plan.UserId != userId)
            return Result.Failure("No tienes permiso para modificar este plan.", "FORBIDDEN");

        meal.IsLocked = request.Locked;
        meal.RowVersion++;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
