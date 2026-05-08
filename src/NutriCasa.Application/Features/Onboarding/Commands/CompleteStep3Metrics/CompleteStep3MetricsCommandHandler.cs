using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Onboarding.DTOs;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep3Metrics;

public class CompleteStep3MetricsCommandHandler : IRequestHandler<CompleteStep3MetricsCommand, Result<CompleteStep3MetricsResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CompleteStep3MetricsCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CompleteStep3MetricsResponse>> Handle(
        CompleteStep3MetricsCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result<CompleteStep3MetricsResponse>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var user = await _context.Users
            .Include(u => u.UserGoals.Where(g => g.IsActive))
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result<CompleteStep3MetricsResponse>.Failure("Usuario no encontrado.", "NOT_FOUND");

        // 1. Actualizar usuario
        user.HeightCm = request.HeightCm;

        // 2. Desactivar metas anteriores
        foreach (var goal in user.UserGoals)
        {
            goal.IsActive = false;
        }

        // 3. Crear nueva meta
        GoalType parsedGoalType = GoalType.WeightLoss; // default
        if (!string.IsNullOrEmpty(request.GoalType) && Enum.TryParse<GoalType>(request.GoalType, out var g))
        {
            parsedGoalType = g;
        }

        var newGoal = new UserGoal
        {
            Id             = Guid.NewGuid(),
            UserId         = userId,
            GoalType       = parsedGoalType,
            StartWeightKg  = request.WeightKg,
            TargetWeightKg = request.TargetWeightKg,
            IsActive       = true
        };
        _context.UserGoals.Add(newGoal);

        // 4. Crear registro en body_measurements
        var measurement = new BodyMeasurement
        {
            Id         = Guid.NewGuid(),
            UserId     = userId,
            MeasuredAt = DateOnly.FromDateTime(DateTime.UtcNow),
            WeightKg   = request.WeightKg
        };
        _context.BodyMeasurements.Add(measurement);

        await _context.SaveChangesAsync(cancellationToken);

        // 5. Warning si la pérdida es > 30%
        string? warningMessage = null;
        if (parsedGoalType == GoalType.WeightLoss && request.TargetWeightKg.HasValue)
        {
            decimal lossPercent = (request.WeightKg - request.TargetWeightKg.Value) / request.WeightKg;
            if (lossPercent > 0.30m)
            {
                warningMessage = "Tu meta de pérdida de peso es bastante ambiciosa (más del 30%). Recuerda hacerlo de forma gradual y saludable.";
            }
        }

        return Result<CompleteStep3MetricsResponse>.Success(new CompleteStep3MetricsResponse
        {
            WarningMessage = warningMessage
        });
    }
}
