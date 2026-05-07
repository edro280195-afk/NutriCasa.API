using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

/// <summary>
/// Stub de almacenamiento en Cloudflare R2.
/// Pendiente de Fase 3 — Storage de fotos en R2.
/// </summary>
public class CloudflareR2StorageService : IFileStorageService
{
    public Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
        => throw new NotImplementedException("Pendiente de Fase 3 — Storage de fotos en R2");

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
        => throw new NotImplementedException("Pendiente de Fase 3 — Storage de fotos en R2");

    public string GetPublicUrl(string storageKey)
        => throw new NotImplementedException("Pendiente de Fase 3 — Storage de fotos en R2");
}
