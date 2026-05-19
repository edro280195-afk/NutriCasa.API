using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.BackgroundJobs;

public class AccountDeletionPurgeJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AccountDeletionPurgeJob> _logger;

    public AccountDeletionPurgeJob(
        IApplicationDbContext context,
        ILogger<AccountDeletionPurgeJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var usersToPurge = await _context.Users
            .Where(u => u.DeletionScheduledFor != null
                     && u.DeletionScheduledFor <= now
                     && u.DeletionCancelledAt == null)
            .ToListAsync(ct);

        if (usersToPurge.Count == 0)
        {
            _logger.LogInformation("AccountDeletionPurge: no hay cuentas para purgar.");
            return;
        }

        foreach (var user in usersToPurge)
        {
            _logger.LogInformation("Purgando cuenta {UserId} (solicitada {Requested}, programada {Scheduled})",
                user.Id, user.DeletionRequestedAt, user.DeletionScheduledFor);
            _context.Users.Remove(user);
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("AccountDeletionPurge: {Count} cuentas purgadas.", usersToPurge.Count);
    }
}
