using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.BackgroundJobs;

public class CheckInReminderJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CheckInReminderJob> _logger;

    public CheckInReminderJob(
        IApplicationDbContext context,
        ILogger<CheckInReminderJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var usersWithCheckInToday = await _context.DailyCheckIns
            .Where(c => c.CheckInDate == today)
            .Select(c => c.UserId)
            .ToListAsync(ct);

        var usersWithoutCheckIn = await _context.Users
            .Where(u => u.DeletedAt == null
                     && !usersWithCheckInToday.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(ct);

        _logger.LogInformation(
            "CheckInReminder: {Total} usuarios sin check-in hoy de {TotalUsers} activos.",
            usersWithoutCheckIn.Count,
            await _context.Users.CountAsync(u => u.DeletedAt == null, ct));
    }
}
