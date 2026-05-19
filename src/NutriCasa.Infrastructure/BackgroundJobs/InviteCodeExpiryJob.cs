using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.BackgroundJobs;

public class InviteCodeExpiryJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<InviteCodeExpiryJob> _logger;

    public InviteCodeExpiryJob(
        IApplicationDbContext context,
        ILogger<InviteCodeExpiryJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var expiredGroups = await _context.Groups
            .Where(g => g.InviteCodeExpiresAt != null
                     && g.InviteCodeExpiresAt <= now
                     && g.InviteCode != null)
            .ToListAsync(ct);

        foreach (var group in expiredGroups)
        {
            _logger.LogInformation("Expirado código de invitación del grupo {GroupId}: {Code}",
                group.Id, group.InviteCode);
            group.InviteCode = "";
            group.InviteCodeExpiresAt = null;
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("InviteCodeExpiry: {Count} códigos expirados.", expiredGroups.Count);
    }
}
