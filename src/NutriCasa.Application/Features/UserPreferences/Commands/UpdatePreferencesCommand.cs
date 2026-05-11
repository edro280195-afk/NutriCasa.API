using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.UserPreferences.Commands;

public record UpdatePreferencesCommand : IRequest<Result>
{
    /* Privacy */
    public string? ShareWeight { get; init; }
    public string? ShareBodyFat { get; init; }
    public string? ShareMeasurements { get; init; }
    public string? SharePhotos { get; init; }
    public string? ShareCheckIns { get; init; }
    public bool? AllowAiMentions { get; init; }

    /* Notifications */
    public bool? AllowPush { get; init; }
    public bool? AllowEmail { get; init; }
    public bool? WeeklyDigest { get; init; }
    public string? QuietHoursStart { get; init; }
    public string? QuietHoursEnd { get; init; }

    /* User settings */
    public string? Timezone { get; init; }
    public string? PreferredLanguage { get; init; }
}

public class UpdatePreferencesCommandHandler : IRequestHandler<UpdatePreferencesCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdatePreferencesCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdatePreferencesCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var user = await _context.Users
            .Include(u => u.PrivacySettings)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return Result.Failure("Usuario no encontrado.", "NOT_FOUND");

        /* User settings */
        if (!string.IsNullOrWhiteSpace(request.Timezone))
            user.Timezone = request.Timezone;

        if (!string.IsNullOrWhiteSpace(request.PreferredLanguage))
            user.PreferredLanguage = request.PreferredLanguage;

        /* Privacy settings */
        var privacy = user.PrivacySettings;
        if (privacy is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.ShareWeight) && Enum.TryParse<VisibilityLevel>(request.ShareWeight, true, out var sw))
                privacy.ShareWeight = sw;
            if (!string.IsNullOrWhiteSpace(request.ShareBodyFat) && Enum.TryParse<VisibilityLevel>(request.ShareBodyFat, true, out var sbf))
                privacy.ShareBodyFat = sbf;
            if (!string.IsNullOrWhiteSpace(request.ShareMeasurements) && Enum.TryParse<VisibilityLevel>(request.ShareMeasurements, true, out var sm))
                privacy.ShareMeasurements = sm;
            if (!string.IsNullOrWhiteSpace(request.SharePhotos) && Enum.TryParse<VisibilityLevel>(request.SharePhotos, true, out var sp))
                privacy.SharePhotos = sp;
            if (!string.IsNullOrWhiteSpace(request.ShareCheckIns) && Enum.TryParse<VisibilityLevel>(request.ShareCheckIns, true, out var sci))
                privacy.ShareCheckIns = sci;
            if (request.AllowAiMentions.HasValue)
                privacy.AllowAiMentions = request.AllowAiMentions.Value;
            if (request.AllowPush.HasValue)
                privacy.AllowPush = request.AllowPush.Value;
            if (request.AllowEmail.HasValue)
                privacy.AllowEmail = request.AllowEmail.Value;
            if (request.WeeklyDigest.HasValue)
                privacy.WeeklyDigest = request.WeeklyDigest.Value;
            if (!string.IsNullOrWhiteSpace(request.QuietHoursStart) && TimeOnly.TryParse(request.QuietHoursStart, out var qs))
                privacy.QuietHoursStart = qs;
            if (!string.IsNullOrWhiteSpace(request.QuietHoursEnd) && TimeOnly.TryParse(request.QuietHoursEnd, out var qe))
                privacy.QuietHoursEnd = qe;

            privacy.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
