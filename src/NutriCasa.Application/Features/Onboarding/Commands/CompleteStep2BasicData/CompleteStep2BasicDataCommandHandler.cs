using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep2BasicData;

public class CompleteStep2BasicDataCommandHandler : IRequestHandler<CompleteStep2BasicDataCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CompleteStep2BasicDataCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(
        CompleteStep2BasicDataCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId, cancellationToken);

        if (user is null)
            return Result.Failure("Usuario no encontrado.", "NOT_FOUND");

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }

        user.BirthDate = request.BirthDate;
        user.Gender    = Enum.Parse<Gender>(request.Gender);

        if (request.ProfilePhotoUrl is not null)
        {
            user.ProfilePhotoUrl = request.ProfilePhotoUrl;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
