using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Progress.DTOs;

namespace NutriCasa.Application.Features.Progress.Queries;

public record GetPhotosQuery : IRequest<Result<List<ProgressPhotoDto>>>;

public class GetPhotosQueryHandler : IRequestHandler<GetPhotosQuery, Result<List<ProgressPhotoDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _storage;

    public GetPhotosQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IFileStorageService storage)
    {
        _context = context;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<Result<List<ProgressPhotoDto>>> Handle(GetPhotosQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<List<ProgressPhotoDto>>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var rows = await _context.ProgressPhotos
            .Where(p => p.UserId == userId && p.DeletedAt == null)
            .OrderByDescending(p => p.TakenAt)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.PhotoUrl,
                p.StorageKey,
                p.Angle,
                p.Visibility,
                p.TakenAt,
                p.CreatedAt,
                p.FileSizeBytes,
            })
            .ToListAsync(ct);

        // Calcular la URL pública dinámicamente desde StorageKey para que
        // siempre refleje la configuración actual (PublicBaseUrl en R2).
        var photos = rows.Select(p => new ProgressPhotoDto
        {
            PhotoId = p.Id,
            PhotoUrl = !string.IsNullOrWhiteSpace(p.StorageKey)
                ? _storage.GetPublicUrl(p.StorageKey)
                : p.PhotoUrl,
            StorageKey = p.StorageKey,
            Angle = p.Angle != null ? p.Angle.ToString() : null,
            Visibility = p.Visibility.ToString(),
            TakenAt = p.TakenAt.ToString("yyyy-MM-dd"),
            CreatedAt = p.CreatedAt,
            FileSizeBytes = p.FileSizeBytes,
        }).ToList();

        return Result<List<ProgressPhotoDto>>.Success(photos);
    }
}
