using MediatR;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Progress.DTOs;
using NutriCasa.Domain.Constants;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace NutriCasa.Application.Features.Progress.Commands;

public record UploadPhotoCommand : IRequest<Result<UploadPhotoResultDto>>
{
    public Stream FileStream { get; init; } = null!;
    public string FileName { get; init; } = "";
    public string ContentType { get; init; } = "";
    public long FileSize { get; init; }
    public string? Angle { get; init; }
    public string Visibility { get; init; } = "Private";
    public string TakenAt { get; init; } = "";
}

public class UploadPhotoCommandHandler : IRequestHandler<UploadPhotoCommand, Result<UploadPhotoResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _storage;

    public UploadPhotoCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IFileStorageService storage)
    {
        _context = context;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<Result<UploadPhotoResultDto>> Handle(UploadPhotoCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<UploadPhotoResultDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        if (request.FileStream is null || request.FileSize == 0)
            return Result<UploadPhotoResultDto>.Failure("El archivo está vacío.", "EMPTY_FILE");

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return Result<UploadPhotoResultDto>.Failure("Formato no permitido. Usa JPG, PNG o WebP.", "INVALID_FORMAT");

        if (request.FileSize > 10 * 1024 * 1024)
            return Result<UploadPhotoResultDto>.Failure("La imagen supera los 10 MB.", "FILE_TOO_LARGE");

        PhotoAngle? angle = null;
        if (!string.IsNullOrWhiteSpace(request.Angle) && Enum.TryParse<PhotoAngle>(request.Angle, true, out var parsed))
            angle = parsed;

        if (!Enum.TryParse<VisibilityLevel>(request.Visibility, true, out var visibility))
            visibility = VisibilityLevel.Private;

        DateOnly takenAt;
        if (!DateOnly.TryParse(request.TakenAt, out takenAt))
            takenAt = DateOnly.FromDateTime(DateTime.UtcNow);

        var maxPhotosPerDay = await _context.SystemThresholds
            .Where(t => t.Code == SystemThresholdCodes.MaxPhotosPerUserDay && t.IsActive)
            .Select(t => (int?)(t.NumericValue ?? 20))
            .FirstOrDefaultAsync(ct) ?? 20;

        var photosToday = await _context.ProgressPhotos
            .CountAsync(p => p.UserId == userId && p.TakenAt == takenAt && p.DeletedAt == null, ct);

        if (photosToday >= maxPhotosPerDay)
            return Result<UploadPhotoResultDto>.Failure(
                $"Ya alcanzaste el limite de {maxPhotosPerDay} fotos para ese dia.",
                "PHOTO_DAILY_LIMIT_REACHED");

        var storageKey = await _storage.UploadAsync(request.FileStream, request.FileName, request.ContentType, ct);
        var photoUrl = _storage.GetPublicUrl(storageKey);

        var photo = new ProgressPhoto
        {
            UserId = userId,
            PhotoUrl = photoUrl,
            StorageKey = storageKey,
            Angle = angle,
            Visibility = visibility,
            FileSizeBytes = request.FileSize,
            TakenAt = takenAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.ProgressPhotos.Add(photo);
        await _context.SaveChangesAsync(ct);

        return Result<UploadPhotoResultDto>.Success(new UploadPhotoResultDto
        {
            PhotoId = photo.Id,
            PhotoUrl = photoUrl,
            StorageKey = storageKey,
            TakenAt = takenAt.ToString("yyyy-MM-dd"),
        });
    }
}
