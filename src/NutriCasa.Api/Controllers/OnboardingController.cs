using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Onboarding.Commands.CompleteStep1Group;
using NutriCasa.Application.Features.Onboarding.Commands.CompleteStep2BasicData;
using NutriCasa.Application.Features.Onboarding.Commands.CompleteStep3Metrics;
using NutriCasa.Application.Features.Onboarding.Commands.CompleteStep4BodyType;
using NutriCasa.Application.Features.Onboarding.Commands.CompleteStep5Activity;
using NutriCasa.Application.Features.Onboarding.Commands.CompleteStep5_5BudgetMode;
using NutriCasa.Application.Features.Onboarding.Commands.CompleteStep6MedicalProfile;
using NutriCasa.Application.Features.Onboarding.Commands.CompleteStep6_5MedicalOverride;
using NutriCasa.Application.Features.Onboarding.Commands.CompleteStep7DisclaimerGoal;
using NutriCasa.Application.Features.Onboarding.DTOs;
using NutriCasa.Application.Features.Onboarding.Queries;

namespace NutriCasa.Api.Controllers;

public class OnboardingController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;

    public OnboardingController(IMediator mediator, IApplicationDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    [HttpPost("step1-group")]
    [Authorize]
    public async Task<IActionResult> Step1Group([FromBody] CompleteStep1GroupRequest request, CancellationToken ct)
    {
        var command = new CompleteStep1GroupCommand
        {
            Action = request.Action,
            GroupName = request.GroupName,
            InviteCode = request.InviteCode
        };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("step2-basic-data")]
    [Authorize]
    public async Task<IActionResult> Step2BasicData([FromBody] CompleteStep2BasicDataRequest request, CancellationToken ct)
    {
        var command = new CompleteStep2BasicDataCommand
        {
            FullName = request.FullName,
            ProfilePhotoUrl = request.ProfilePhotoUrl,
            BirthDate = request.BirthDate,
            Gender = request.Gender
        };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("step3-metrics")]
    [Authorize]
    public async Task<IActionResult> Step3Metrics([FromBody] CompleteStep3MetricsRequest request, CancellationToken ct)
    {
        var command = new CompleteStep3MetricsCommand
        {
            HeightCm = request.HeightCm,
            WeightKg = request.WeightKg,
            TargetWeightKg = request.TargetWeightKg,
            GoalType = request.GoalType
        };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("step4-body-type")]
    [Authorize]
    public async Task<IActionResult> Step4BodyType([FromBody] CompleteStep4BodyTypeRequest request, CancellationToken ct)
    {
        var command = new CompleteStep4BodyTypeCommand { BodyType = request.BodyType };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("step5-activity")]
    [Authorize]
    public async Task<IActionResult> Step5Activity([FromBody] CompleteStep5ActivityRequest request, CancellationToken ct)
    {
        var command = new CompleteStep5ActivityCommand { ActivityLevel = request.ActivityLevel };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("step5-budget-mode")]
    [Authorize]
    public async Task<IActionResult> Step5BudgetMode([FromBody] CompleteStep5_5BudgetModeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.BudgetModeCode))
            return HandleResult(Result.Failure("El código de presupuesto es requerido.", "VALIDATION_ERROR"));

        var budgetMode = await _context.BudgetModes
            .FirstOrDefaultAsync(b => b.Code == request.BudgetModeCode && b.IsActive, ct);
        if (budgetMode is null)
            return HandleResult(Result.Failure("Modo de presupuesto no encontrado.", "INVALID_BUDGET_MODE"));

        var command = new CompleteStep5_5BudgetModeCommand { BudgetModeId = budgetMode.Id };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("step6-medical-profile")]
    [Authorize]
    public async Task<IActionResult> Step6MedicalProfile([FromBody] CompleteStep6MedicalProfileRequest request, CancellationToken ct)
    {
        var command = new CompleteStep6MedicalProfileCommand
        {
            HasDiabetes = request.HasDiabetes,
            DiabetesType = request.DiabetesType,
            IsPregnantOrLactating = request.IsPregnantOrLactating,
            HasKidneyIssues = request.HasKidneyIssues,
            HasLiverIssues = request.HasLiverIssues,
            HasPancreasIssues = request.HasPancreasIssues,
            HasThyroidIssues = request.HasThyroidIssues,
            HasHeartCondition = request.HasHeartCondition,
            HasEatingDisorderHistory = request.HasEatingDisorderHistory,
            HasGallbladderIssues = request.HasGallbladderIssues,
            OtherConditions = request.OtherConditions,
            Allergies = request.Allergies,
            Medications = request.Medications,
            DietaryRestrictions = request.DietaryRestrictions,
            DislikedIngredients = request.DislikedIngredients,
            PreferredIngredients = request.PreferredIngredients,
            KetoExperienceLevel = request.KetoExperienceLevel
        };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("step6-override")]
    [Authorize]
    public async Task<IActionResult> Step6Override([FromBody] CompleteStep6_5MedicalOverrideRequest request, CancellationToken ct)
    {
        var command = new CompleteStep6_5MedicalOverrideCommand
        {
            PasswordConfirmation = request.PasswordConfirmation,
            DisclaimerAccepted = request.DisclaimerAccepted,
            DisclaimerVersionId = request.DisclaimerVersionId
        };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("step7-disclaimer-goal")]
    [Authorize]
    public async Task<IActionResult> Step7DisclaimerGoal([FromBody] CompleteStep7DisclaimerGoalRequest request, CancellationToken ct)
    {
        Guid? disclaimerVersionId = null;
        if (!string.IsNullOrEmpty(request.DisclaimerVersionId)
            && Guid.TryParse(request.DisclaimerVersionId, out var parsed))
        {
            disclaimerVersionId = parsed;
        }

        var command = new CompleteStep7DisclaimerGoalCommand
        {
            DisclaimerVersionId = disclaimerVersionId,
            GoalType = request.GoalType,
            TargetWeightKg = request.TargetWeightKg,
            TargetDate = request.TargetDate,
            MotivationText = request.MotivationText
        };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var query = new OnboardingStatusQuery();
        var result = await _mediator.Send(query, ct);
        return HandleResult(result);
    }
}
