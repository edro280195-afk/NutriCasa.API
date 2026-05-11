using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.PushSubscriptions.DTOs;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.PushSubscriptions.Commands;

public record SubscribeCommand : IRequest<Result<PushSubscriptionDto>>
{
    public required string Endpoint { get; init; }
    public required string P256dhKey { get; init; }
    public required string AuthKey { get; init; }
}

public class SubscribeCommandHandler : IRequestHandler<SubscribeCommand, Result<PushSubscriptionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SubscribeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PushSubscriptionDto>> Handle(SubscribeCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<PushSubscriptionDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var existing = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == request.Endpoint, ct);

        if (existing is not null)
        {
            existing.P256dhKey = request.P256dhKey;
            existing.AuthKey = request.AuthKey;
            existing.IsActive = true;
            existing.LastUsedAt = DateTime.UtcNow;
            existing.UserAgent = _currentUser.UserAgent;
            await _context.SaveChangesAsync(ct);

            return Result<PushSubscriptionDto>.Success(new PushSubscriptionDto
            {
                Id = existing.Id,
                Endpoint = existing.Endpoint,
                IsActive = existing.IsActive,
                CreatedAt = existing.CreatedAt,
                LastUsedAt = existing.LastUsedAt,
            });
        }

        var sub = new PushSubscription
        {
            UserId = userId,
            Endpoint = request.Endpoint,
            P256dhKey = request.P256dhKey,
            AuthKey = request.AuthKey,
            UserAgent = _currentUser.UserAgent,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
        };

        _context.PushSubscriptions.Add(sub);
        await _context.SaveChangesAsync(ct);

        return Result<PushSubscriptionDto>.Success(new PushSubscriptionDto
        {
            Id = sub.Id,
            Endpoint = sub.Endpoint,
            IsActive = sub.IsActive,
            CreatedAt = sub.CreatedAt,
            LastUsedAt = sub.LastUsedAt,
        });
    }
}
