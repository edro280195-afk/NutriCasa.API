using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.UserMedicalProfile.DTOs;

namespace NutriCasa.Application.Features.UserMedicalProfile.Queries;

public record GetMedicalProfileQuery : IRequest<Result<MedicalProfileDto>>;

public class GetMedicalProfileQueryHandler : IRequestHandler<GetMedicalProfileQuery, Result<MedicalProfileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMedicalProfileQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<MedicalProfileDto>> Handle(GetMedicalProfileQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<MedicalProfileDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var profile = await _context.MedicalProfiles
            .FirstOrDefaultAsync(mp => mp.UserId == _currentUser.UserId.Value, ct);

        if (profile is null)
            return Result<MedicalProfileDto>.Failure("Perfil médico no encontrado. Completa el onboarding primero.", "NOT_FOUND");

        return Result<MedicalProfileDto>.Success(new MedicalProfileDto
        {
            HasDiabetes = profile.HasDiabetes,
            DiabetesType = profile.DiabetesType?.ToString(),
            IsPregnantOrLactating = profile.IsPregnantOrLactating,
            HasKidneyIssues = profile.HasKidneyIssues,
            HasLiverIssues = profile.HasLiverIssues,
            HasPancreasIssues = profile.HasPancreasIssues,
            HasThyroidIssues = profile.HasThyroidIssues,
            HasHeartCondition = profile.HasHeartCondition,
            HasEatingDisorderHistory = profile.HasEatingDisorderHistory,
            HasGallbladderIssues = profile.HasGallbladderIssues,
            OtherConditions = profile.OtherConditions,
            Allergies = profile.Allergies,
            Medications = profile.Medications,
            DietaryRestrictions = profile.DietaryRestrictions,
            DislikedIngredients = profile.DislikedIngredients,
            PreferredIngredients = profile.PreferredIngredients,
            KetoExperienceLevel = profile.KetoExperienceLevel.ToString(),
            RequiresHumanReview = profile.RequiresHumanReview,
            OverrideAcceptedAt = profile.OverrideAcceptedAt,
        });
    }
}
