using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Onboarding.DTOs;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep6MedicalProfile;

public class CompleteStep6MedicalProfileCommandHandler : IRequestHandler<CompleteStep6MedicalProfileCommand, Result<CompleteStep6MedicalProfileResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CompleteStep6MedicalProfileCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CompleteStep6MedicalProfileResponse>> Handle(CompleteStep6MedicalProfileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result<CompleteStep6MedicalProfileResponse>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var existingProfile = await _context.MedicalProfiles
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);

        bool requiresReview = CalculateRequiresHumanReview(request);
        var conditions = GetBlockingConditions(request);

        if (existingProfile is not null)
        {
            existingProfile.HasDiabetes = request.HasDiabetes;
            existingProfile.DiabetesType = !string.IsNullOrEmpty(request.DiabetesType) ? Enum.Parse<DiabetesType>(request.DiabetesType) : null;
            existingProfile.IsPregnantOrLactating = request.IsPregnantOrLactating;
            existingProfile.HasKidneyIssues = request.HasKidneyIssues;
            existingProfile.HasLiverIssues = request.HasLiverIssues;
            existingProfile.HasPancreasIssues = request.HasPancreasIssues;
            existingProfile.HasThyroidIssues = request.HasThyroidIssues;
            existingProfile.HasHeartCondition = request.HasHeartCondition;
            existingProfile.HasEatingDisorderHistory = request.HasEatingDisorderHistory;
            existingProfile.HasGallbladderIssues = request.HasGallbladderIssues;
            existingProfile.OtherConditions = request.OtherConditions;
            existingProfile.Allergies = request.Allergies;
            existingProfile.Medications = request.Medications;
            existingProfile.DietaryRestrictions = request.DietaryRestrictions;
            existingProfile.DislikedIngredients = request.DislikedIngredients;
            existingProfile.PreferredIngredients = request.PreferredIngredients;
            existingProfile.KetoExperienceLevel = Enum.Parse<KetoExperienceLevel>(request.KetoExperienceLevel);
            existingProfile.RequiresHumanReview = requiresReview;
        }
        else
        {
            var profile = new MedicalProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                HasDiabetes = request.HasDiabetes,
                DiabetesType = !string.IsNullOrEmpty(request.DiabetesType) ? Enum.Parse<DiabetesType>(request.DiabetesType) : null,
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
                KetoExperienceLevel = Enum.Parse<KetoExperienceLevel>(request.KetoExperienceLevel),
                RequiresHumanReview = requiresReview,
            };
            _context.MedicalProfiles.Add(profile);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<CompleteStep6MedicalProfileResponse>.Success(new CompleteStep6MedicalProfileResponse
        {
            RequiresOverride = requiresReview,
            Conditions = conditions,
            Message = requiresReview
                ? "Detectamos una condición que requiere validación adicional."
                : null
        });
    }

    private static bool CalculateRequiresHumanReview(CompleteStep6MedicalProfileCommand request)
    {
        if (request.IsPregnantOrLactating) return true;
        if (request.HasKidneyIssues) return true;
        if (request.HasLiverIssues) return true;
        if (request.HasPancreasIssues) return true;
        if (request.HasHeartCondition) return true;
        if (request.HasEatingDisorderHistory) return true;
        if (request.HasDiabetes && request.DiabetesType == "T1") return true;

        return false;
    }

    private static string[] GetBlockingConditions(CompleteStep6MedicalProfileCommand request)
    {
        var conditions = new List<string>();
        if (request.HasDiabetes && request.DiabetesType == "T1") conditions.Add("diabetes_t1");
        if (request.IsPregnantOrLactating) conditions.Add("pregnancy_or_lactating");
        if (request.HasKidneyIssues) conditions.Add("kidney_issues");
        if (request.HasLiverIssues) conditions.Add("liver_issues");
        if (request.HasPancreasIssues) conditions.Add("pancreas_issues");
        if (request.HasHeartCondition) conditions.Add("heart_condition");
        if (request.HasEatingDisorderHistory) conditions.Add("eating_disorder_history");
        return conditions.ToArray();
    }
}
