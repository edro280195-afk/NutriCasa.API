using MediatR;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Family.Commands;

public record UploadPostImageCommand : IRequest<Result<string>>
{
    public required Stream FileStream { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSize { get; init; }
}

public class UploadPostImageCommandHandler : IRequestHandler<UploadPostImageCommand, Result<string>>
{
    private readonly IFileStorageService _storage;
    private readonly ICurrentUserService _currentUser;

    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public UploadPostImageCommandHandler(IFileStorageService storage, ICurrentUserService currentUser)
    {
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<Result<string>> Handle(UploadPostImageCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<string>.Failure("No autenticado.", "UNAUTHORIZED");

        if (request.FileSize == 0)
            return Result<string>.Failure("El archivo está vacío.", "EMPTY_FILE");

        if (request.FileSize > 10 * 1024 * 1024)
            return Result<string>.Failure("El archivo excede el límite de 10 MB.", "FILE_TOO_LARGE");

        var ext = Path.GetExtension(request.FileName);
        if (!Allowed.Contains(ext))
            return Result<string>.Failure("Formato no permitido. Usa JPG, PNG, WebP o GIF.", "INVALID_FORMAT");

        var storageKey = $"family/{_currentUser.UserId}/{Guid.NewGuid()}{ext}";
        await _storage.UploadAsync(request.FileStream, storageKey, request.ContentType, ct);
        var url = _storage.GetPublicUrl(storageKey);

        return Result<string>.Success(url);
    }
}
