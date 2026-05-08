using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Auth.Commands.RegisterUser;

namespace NutriCasa.Application.Features.Auth.Commands.Logout;

public record LogoutCommand : IRequest<Result>
{
    public required string RefreshToken { get; init; }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public LogoutCommandHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        string tokenHash = RegisterUserCommandHandler.ComputeSha256Hash(request.RefreshToken);

        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt =>
                rt.TokenHash == tokenHash && rt.RevokedAt == null,
                cancellationToken);

        if (token is not null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Siempre éxito — el cliente debe borrar los tokens localmente
        return Result.Success();
    }
}
