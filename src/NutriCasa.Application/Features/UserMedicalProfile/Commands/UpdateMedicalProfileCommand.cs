using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.UserMedicalProfile.Commands;

public record UpdateMedicalProfileCommand : IRequest<Result>
{
    public bool HasDiabetes { get; init; }
    public string? DiabetesType { get; init; }
    public bool IsPregnantOrLactating { get; init; }
    public bool HasKidneyIssues { get; init; }
    public bool HasLiverIssues { get; init; }
    public bool HasPancreasIssues { get; init; }
    public bool HasThyroidIssues { get; init; }
    public bool HasHeartCondition { get; init; }
    public bool HasEatingDisorderHistory { get; init; }
    public bool HasGallbladderIssues { get; init; }
    public string? OtherConditions { get; init; }
    public string[] Allergies { get; init; } = [];
    public string[] Medications { get; init; } = [];
    public string[] DietaryRestrictions { get; init; } = [];
    public string[] DislikedIngredients { get; init; } = [];
    public string[] PreferredIngredients { get; init; } = [];
    public string KetoExperienceLevel { get; init; } = "Beginner";
}

public class UpdateMedicalProfileCommandHandler : IRequestHandler<UpdateMedicalProfileCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateMedicalProfileCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateMedicalProfileCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var profile = await _context.MedicalProfiles
            .FirstOrDefaultAsync(mp => mp.UserId == userId, ct);

        if (profile is null)
            return Result.Failure("Perfil médico no encontrado.", "NOT_FOUND");

        if (!Enum.TryParse<KetoExperienceLevel>(request.KetoExperienceLevel, true, out var expLevel))
            expLevel = KetoExperienceLevel.Beginner;

        DiabetesType? diabetesType = null;
        if (!string.IsNullOrEmpty(request.DiabetesType) && Enum.TryParse<DiabetesType>(request.DiabetesType, true, out var dt))
            diabetesType = dt;

        profile.HasDiabetes = request.HasDiabetes;
        profile.DiabetesType = diabetesType;
        profile.IsPregnantOrLactating = request.IsPregnantOrLactating;
        profile.HasKidneyIssues = request.HasKidneyIssues;
        profile.HasLiverIssues = request.HasLiverIssues;
        profile.HasPancreasIssues = request.HasPancreasIssues;
        profile.HasThyroidIssues = request.HasThyroidIssues;
        profile.HasHeartCondition = request.HasHeartCondition;
        profile.HasEatingDisorderHistory = request.HasEatingDisorderHistory;
        profile.HasGallbladderIssues = request.HasGallbladderIssues;
        profile.OtherConditions = request.OtherConditions;
        profile.Allergies = request.Allergies;
        profile.Medications = request.Medications;
        profile.DietaryRestrictions = request.DietaryRestrictions;
        profile.DislikedIngredients = request.DislikedIngredients;
        profile.PreferredIngredients = request.PreferredIngredients;
        profile.KetoExperienceLevel = expLevel;

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
