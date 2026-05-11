using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Admin.Commands.UpdateUserRole;

public record UpdateUserRoleCommand : IRequest<Result>
{
    public required Guid UserId { get; init; }
    public required string Role { get; init; }
}

public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserRoleCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure("Usuario no encontrado.", "NOT_FOUND");

        if (request.Role != "user" && request.Role != "admin")
            return Result.Failure("Rol inválido. Usa 'user' o 'admin'.", "INVALID_ROLE");

        user.Role = request.Role;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
