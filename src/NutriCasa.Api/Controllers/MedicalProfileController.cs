using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.UserMedicalProfile.Commands;
using NutriCasa.Application.Features.UserMedicalProfile.DTOs;
using NutriCasa.Application.Features.UserMedicalProfile.Queries;

namespace NutriCasa.Api.Controllers;

[Authorize]
public class MedicalProfileController : BaseApiController
{
    private readonly IMediator _mediator;

    public MedicalProfileController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMedicalProfileQuery(), ct);
        return HandleResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateMedicalProfileRequest request, CancellationToken ct)
    {
        var command = new UpdateMedicalProfileCommand
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
            KetoExperienceLevel = request.KetoExperienceLevel,
        };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }
}
