using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Notifications.Commands;

public record MarkNotificationReadCommand : IRequest<Result>
{
    public required Guid NotificationId { get; init; }
}

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationReadCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == _currentUser.UserId, ct);

        if (notification is null)
            return Result.Failure("Notificación no encontrada.", "NOT_FOUND");

        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
