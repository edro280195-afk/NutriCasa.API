using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Progress.Commands;

public record DeletePhotoCommand : IRequest<Result>
{
    public Guid PhotoId { get; init; }
}

public class DeletePhotoCommandHandler : IRequestHandler<DeletePhotoCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _storage;

    public DeletePhotoCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IFileStorageService storage)
    {
        _context = context;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<Result> Handle(DeletePhotoCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var photo = await _context.ProgressPhotos
            .FirstOrDefaultAsync(p => p.Id == request.PhotoId && p.UserId == userId && p.DeletedAt == null, ct);

        if (photo is null)
            return Result.Failure("Foto no encontrada.", "NOT_FOUND");

        await _storage.DeleteAsync(photo.StorageKey, ct);

        photo.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
