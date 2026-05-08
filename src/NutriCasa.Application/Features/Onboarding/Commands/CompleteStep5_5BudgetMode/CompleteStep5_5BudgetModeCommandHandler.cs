using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep5_5BudgetMode;

public class CompleteStep5_5BudgetModeCommandHandler : IRequestHandler<CompleteStep5_5BudgetModeCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CompleteStep5_5BudgetModeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(CompleteStep5_5BudgetModeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var budgetMode = await _context.BudgetModes
            .FirstOrDefaultAsync(b => b.Id == request.BudgetModeId && b.IsActive, cancellationToken);

        if (budgetMode is null)
            return Result.Failure("El modo de presupuesto no existe o no está disponible.", "INVALID_BUDGET_MODE");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId, cancellationToken);

        if (user is null)
            return Result.Failure("Usuario no encontrado.", "NOT_FOUND");

        user.BudgetModeId = request.BudgetModeId;
        user.BudgetModeChangedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
