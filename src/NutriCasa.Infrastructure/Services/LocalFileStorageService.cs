using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath;
    private readonly string _baseUrl;

    public LocalFileStorageService(string storagePath, string baseUrl)
    {
        _storagePath = storagePath;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        var storageKey = $"{Guid.NewGuid()}{ext}";

        var dir = Path.Combine(_storagePath, "uploads", "progress");
        Directory.CreateDirectory(dir);

        var fullPath = Path.Combine(dir, storageKey);
        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await fileStream.CopyToAsync(fs, ct);

        return storageKey;
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_storagePath, "uploads", "progress", storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string storageKey)
    {
        return $"{_baseUrl}/uploads/progress/{storageKey}";
    }
}
