using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Family.Queries.GetInviteCode;

public record GetInviteCodeQuery : IRequest<Result<InviteCodeDto>>;

public record InviteCodeDto
{
    public string InviteCode { get; set; } = "";
    public string DeepLink { get; set; } = "";
    public bool IsExpiringSoon { get; set; }
}

public class GetInviteCodeQueryHandler : IRequestHandler<GetInviteCodeQuery, Result<InviteCodeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetInviteCodeQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<InviteCodeDto>> Handle(GetInviteCodeQuery request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<InviteCodeDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var group = await _context.GroupMemberships
            .Include(m => m.Group)
            .Where(m => m.UserId == userId && m.LeftAt == null)
            .Select(m => m.Group)
            .FirstOrDefaultAsync(ct);

        if (group is null)
            return Result<InviteCodeDto>.Failure("No perteneces a ningún grupo.", "NO_GROUP");

        var isExpiringSoon = group.InviteCodeExpiresAt.HasValue
            && group.InviteCodeExpiresAt.Value < DateTime.UtcNow.AddDays(3);

        return Result<InviteCodeDto>.Success(new InviteCodeDto
        {
            InviteCode = group.InviteCode ?? "",
            DeepLink = $"nutricasa://join?code={group.InviteCode}",
            IsExpiringSoon = isExpiringSoon,
        });
    }
}
