using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Family.Commands.RegenerateInviteCode;

public record RegenerateInviteCodeCommand : IRequest<Result>;

public class RegenerateInviteCodeCommandHandler : IRequestHandler<RegenerateInviteCodeCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RegenerateInviteCodeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(RegenerateInviteCodeCommand request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var membership = await _context.GroupMemberships
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (membership is null)
            return Result.Failure("No perteneces a ningún grupo.", "NO_GROUP");

        if (membership.Role != GroupRole.Owner && membership.Role != GroupRole.Admin)
            return Result.Failure("Solo owner o admin pueden regenerar el código de invitación.", "FORBIDDEN");

        var group = membership.Group;
        group.InviteCode = GenerateInviteCode();
        // Código expira en 30 días
        group.InviteCodeExpiresAt = DateTime.UtcNow.AddDays(30);

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static string GenerateInviteCode()
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var letters = new string(Enumerable.Repeat(chars, 4).Select(s => s[random.Next(s.Length)]).ToArray());
        var numbers = random.Next(1000, 10000).ToString();
        return $"NUT-{letters}-{numbers}";
    }
}
