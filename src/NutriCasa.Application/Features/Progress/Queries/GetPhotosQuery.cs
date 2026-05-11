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

    public GetPhotosQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<List<ProgressPhotoDto>>> Handle(GetPhotosQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<List<ProgressPhotoDto>>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var photos = await _context.ProgressPhotos
            .Where(p => p.UserId == userId && p.DeletedAt == null)
            .OrderByDescending(p => p.TakenAt)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => new ProgressPhotoDto
            {
                PhotoId = p.Id,
                PhotoUrl = p.PhotoUrl,
                StorageKey = p.StorageKey,
                Angle = p.Angle != null ? p.Angle.ToString() : null,
                Visibility = p.Visibility.ToString(),
                TakenAt = p.TakenAt.ToString("yyyy-MM-dd"),
                CreatedAt = p.CreatedAt,
                FileSizeBytes = p.FileSizeBytes,
            })
            .ToListAsync(ct);

        return Result<List<ProgressPhotoDto>>.Success(photos);
    }
}
