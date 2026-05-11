using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.PushSubscriptions.Commands;

public record UnsubscribeCommand : IRequest<Result>
{
    public required string Endpoint { get; init; }
}

public class UnsubscribeCommandHandler : IRequestHandler<UnsubscribeCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UnsubscribeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UnsubscribeCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var sub = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == request.Endpoint, ct);

        if (sub is null)
            return Result.Failure("Suscripción no encontrada.", "NOT_FOUND");

        _context.PushSubscriptions.Remove(sub);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
