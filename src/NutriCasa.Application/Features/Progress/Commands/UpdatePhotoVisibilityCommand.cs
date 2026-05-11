using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Progress.Commands;

public record UpdatePhotoVisibilityCommand : IRequest<Result>
{
    public Guid PhotoId { get; init; }
    public string Visibility { get; init; } = "Private";
}

public class UpdatePhotoVisibilityCommandHandler : IRequestHandler<UpdatePhotoVisibilityCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdatePhotoVisibilityCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdatePhotoVisibilityCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var photo = await _context.ProgressPhotos
            .FirstOrDefaultAsync(p => p.Id == request.PhotoId && p.UserId == userId && p.DeletedAt == null, ct);

        if (photo is null)
            return Result.Failure("Foto no encontrada.", "NOT_FOUND");

        if (!Enum.TryParse<VisibilityLevel>(request.Visibility, true, out var visibility))
            return Result.Failure("Visibilidad inválida.", "INVALID_VISIBILITY");

        photo.Visibility = visibility;
        photo.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
