using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<UploadPostImageCommandHandler> _logger;

    private static readonly Dictionary<string, string> ContentTypeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
        ["image/heic"] = ".heic",
        ["image/heif"] = ".heif",
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".heic", ".heif" };

    public UploadPostImageCommandHandler(
        IFileStorageService storage,
        ICurrentUserService currentUser,
        ILogger<UploadPostImageCommandHandler> logger)
    {
        _storage = storage;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(UploadPostImageCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<string>.Failure("No autenticado.", "UNAUTHORIZED");

        if (request.FileSize == 0)
        {
            _logger.LogWarning(
                "Imagen de post rechazada: archivo vacio. UserId={UserId}, FileName={FileName}, ContentType={ContentType}",
                _currentUser.UserId, request.FileName, request.ContentType);
            return Result<string>.Failure("El archivo esta vacio.", "EMPTY_FILE");
        }

        if (request.FileSize > 10 * 1024 * 1024)
        {
            _logger.LogWarning(
                "Imagen de post rechazada: archivo demasiado grande. UserId={UserId}, FileName={FileName}, FileSize={FileSize}",
                _currentUser.UserId, request.FileName, request.FileSize);
            return Result<string>.Failure("El archivo excede el limite de 10 MB.", "FILE_TOO_LARGE");
        }

        var extension = ResolveExtension(request.FileName, request.ContentType);
        if (extension is null)
        {
            _logger.LogWarning(
                "Imagen de post rechazada: formato invalido. UserId={UserId}, FileName={FileName}, ContentType={ContentType}",
                _currentUser.UserId, request.FileName, request.ContentType);
            return Result<string>.Failure("Formato no permitido. Usa JPG, PNG, WebP, GIF o HEIC.", "INVALID_FORMAT");
        }

        var safeFileName = BuildSafeFileName(request.FileName, extension);

        try
        {
            var storageKey = await _storage.UploadAsync(request.FileStream, safeFileName, request.ContentType, ct);
            var url = _storage.GetPublicUrl(storageKey);
            return Result<string>.Success(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al subir imagen de post. UserId={UserId}, FileName={FileName}, ContentType={ContentType}, FileSize={FileSize}",
                _currentUser.UserId, request.FileName, request.ContentType, request.FileSize);
            return Result<string>.Failure("No se pudo subir la imagen. Intenta de nuevo en unos minutos.", "UPLOAD_FAILED");
        }
    }

    private static string? ResolveExtension(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension))
            return extension.ToLowerInvariant();

        return ContentTypeExtensions.TryGetValue(contentType, out var contentTypeExtension)
            ? contentTypeExtension
            : null;
    }

    private static string BuildSafeFileName(string fileName, string extension)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(nameWithoutExtension))
            nameWithoutExtension = "post-image";

        return $"{nameWithoutExtension}{extension}";
    }
}
