using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Onboarding.DTOs;

namespace NutriCasa.Application.Features.Onboarding.Queries;

public record OnboardingStatusQuery : IRequest<Result<OnboardingStatusResponse>>;

public class OnboardingStatusQueryHandler : IRequestHandler<OnboardingStatusQuery, Result<OnboardingStatusResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public OnboardingStatusQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<OnboardingStatusResponse>> Handle(OnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result<OnboardingStatusResponse>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var user = await _context.Users
            .Include(u => u.MedicalProfile)
            .Include(u => u.GroupMemberships)
            .Include(u => u.UserGoals)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result<OnboardingStatusResponse>.Failure("Usuario no encontrado.", "NOT_FOUND");

        bool step1 = user.GroupMemberships.Any();
        bool step2 = !string.IsNullOrEmpty(user.FullName) && user.BirthDate != default;
        bool step3 = user.HeightCm > 0 && user.UserGoals.Any(g => g.IsActive);
        bool step4 = !string.IsNullOrEmpty(user.BodyTypeSelected);
        bool step5 = user.ActivityLevel != Domain.Enums.ActivityLevel.Sedentary || user.HeightCm > 0;
        bool step5Budget = user.BudgetModeId.HasValue;
        bool step6 = user.MedicalProfile is not null;
        bool step6Override = user.MedicalProfile?.OverrideAcceptedAt is not null;
        bool step7 = user.DisclaimerAcceptedAt.HasValue;

        bool onboardingComplete = step1 && step2 && step3 && step4 && step5 && step5Budget && step6 && step7;

        bool requiresOverride = user.MedicalProfile?.RequiresHumanReview == true;

        int currentStep = CalculateCurrentStep(step1, step2, step3, step4, step5, step5Budget, step6, step6Override, step7, requiresOverride);

        return Result<OnboardingStatusResponse>.Success(new OnboardingStatusResponse
        {
            StepsCompleted = new StepsCompletedDto
            {
                Step1Group = step1,
                Step2BasicData = step2,
                Step3Metrics = step3,
                Step4BodyType = step4,
                Step5Activity = step5,
                Step5BudgetMode = step5Budget,
                Step6MedicalProfile = step6,
                Step6Override = step6Override,
                Step7Disclaimer = step7,
            },
            RequiresOverride = requiresOverride,
            OnboardingComplete = onboardingComplete,
            CurrentSuggestedStep = currentStep,
        });
    }

    private static int CalculateCurrentStep(
        bool s1, bool s2, bool s3, bool s4, bool s5, bool s5b, bool s6, bool s6o, bool s7, bool reqOverride)
    {
        if (!s1) return 1;
        if (!s2) return 2;
        if (!s3) return 3;
        if (!s4) return 4;
        if (!s5) return 5;
        if (!s5b) return 6; // 5.5
        if (!s6) return 7;  // 6
        if (reqOverride && !s6o) return 8; // 6.5
        if (!s7) return 9;  // 7
        return 10; // complete
    }
}
