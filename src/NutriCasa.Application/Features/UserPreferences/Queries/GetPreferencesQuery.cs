using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.UserPreferences.DTOs;

namespace NutriCasa.Application.Features.UserPreferences.Queries;

public record GetPreferencesQuery : IRequest<Result<PreferencesDto>>;

public class GetPreferencesQueryHandler : IRequestHandler<GetPreferencesQuery, Result<PreferencesDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetPreferencesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PreferencesDto>> Handle(GetPreferencesQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<PreferencesDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var user = await _context.Users
            .Include(u => u.PrivacySettings)
            .Include(u => u.BudgetMode)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return Result<PreferencesDto>.Failure("Usuario no encontrado.", "NOT_FOUND");

        var privacy = user.PrivacySettings;

        return Result<PreferencesDto>.Success(new PreferencesDto
        {
            /* Privacy */
            ShareWeight = privacy?.ShareWeight.ToString() ?? "Private",
            ShareBodyFat = privacy?.ShareBodyFat.ToString() ?? "Private",
            ShareMeasurements = privacy?.ShareMeasurements.ToString() ?? "Private",
            SharePhotos = privacy?.SharePhotos.ToString() ?? "Private",
            ShareCheckIns = privacy?.ShareCheckIns.ToString() ?? "Group",
            AllowAiMentions = privacy?.AllowAiMentions ?? true,

            /* Notifications */
            AllowPush = privacy?.AllowPush ?? true,
            AllowEmail = privacy?.AllowEmail ?? true,
            WeeklyDigest = privacy?.WeeklyDigest ?? true,
            QuietHoursStart = privacy?.QuietHoursStart.ToString() ?? "21:00",
            QuietHoursEnd = privacy?.QuietHoursEnd.ToString() ?? "08:00",

            /* User settings */
            Timezone = user.Timezone,
            PreferredLanguage = user.PreferredLanguage,
            NutritionTrack = user.NutritionTrack.ToString(),
            BudgetModeCode = user.BudgetMode?.Code,
            BudgetModeName = user.BudgetMode?.Name,
        });
    }
}
