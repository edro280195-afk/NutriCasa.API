using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Admin.DTOs;

namespace NutriCasa.Application.Features.Admin.Queries.GetUsers;

public record GetUsersQuery : IRequest<Result<List<AdminUserDto>>>
{
    public string? Search { get; init; }
    public string? Role { get; init; }
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<List<AdminUserDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetUsersQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<AdminUserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(search)
                                  || u.Email.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
            query = query.Where(u => u.Role == request.Role);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                EmailVerified = u.EmailVerifiedAt != null,
                OnboardingComplete = u.DisclaimerAcceptedAt != null,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Result<List<AdminUserDto>>.Success(users);
    }
}
