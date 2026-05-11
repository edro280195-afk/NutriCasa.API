using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Notifications.DTOs;

namespace NutriCasa.Application.Features.Notifications.Queries;

public record GetUnreadCountQuery : IRequest<Result<UnreadCountDto>>;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, Result<UnreadCountDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetUnreadCountQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<UnreadCountDto>> Handle(GetUnreadCountQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<UnreadCountDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var count = await _context.Notifications
            .CountAsync(n => n.UserId == userId && n.ReadAt == null, ct);

        return Result<UnreadCountDto>.Success(new UnreadCountDto { Count = count });
    }
}
