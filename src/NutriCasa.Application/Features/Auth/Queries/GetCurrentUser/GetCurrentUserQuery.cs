using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Auth.DTOs;

namespace NutriCasa.Application.Features.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<Result<CurrentUserResponse>>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CurrentUserResponse>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result<CurrentUserResponse>.Failure("No autenticado.", "UNAUTHORIZED");

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId, cancellationToken);

        if (user is null)
            return Result<CurrentUserResponse>.Failure("Usuario no encontrado.", "NOT_FOUND");

        return Result<CurrentUserResponse>.Success(new CurrentUserResponse
        {
            UserId             = user.Id,
            FullName           = user.FullName,
            Email              = user.Email,
            EmailVerified      = user.EmailVerifiedAt.HasValue,
            OnboardingComplete = user.DisclaimerAcceptedAt.HasValue,
            ProfilePhotoUrl    = user.ProfilePhotoUrl,
            Gender             = user.Gender.ToString(),
            BirthDate          = user.BirthDate,
            HeightCm           = user.HeightCm > 0 ? user.HeightCm : null,
            LastLoginAt        = user.LastLoginAt
        });
    }
}
