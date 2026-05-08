using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep5Activity;

public class CompleteStep5ActivityCommandHandler : IRequestHandler<CompleteStep5ActivityCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CompleteStep5ActivityCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(CompleteStep5ActivityCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId, cancellationToken);

        if (user is null)
            return Result.Failure("Usuario no encontrado.", "NOT_FOUND");

        user.ActivityLevel = Enum.Parse<ActivityLevel>(request.ActivityLevel);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
