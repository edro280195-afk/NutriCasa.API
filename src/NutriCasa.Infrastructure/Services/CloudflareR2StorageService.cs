using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

public class CloudflareR2StorageService : IFileStorageService
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;
    private readonly string _publicBaseUrl;
    private readonly string _keyPrefix;

    public CloudflareR2StorageService(IConfiguration configuration)
    {
        var section = configuration.GetSection("Storage:R2");
        var accountId = section["AccountId"]?.Trim();
        var endpointUrl = section["EndpointUrl"]?.Trim();
        var accessKeyId = section["AccessKeyId"];
        var secretAccessKey = section["SecretAccessKey"];

        _bucketName = section["BucketName"] ?? throw new InvalidOperationException("Storage:R2:BucketName no configurado.");
        _publicBaseUrl = (section["PublicBaseUrl"] ?? throw new InvalidOperationException("Storage:R2:PublicBaseUrl no configurado.")).TrimEnd('/');
        _keyPrefix = (section["KeyPrefix"] ?? "uploads/progress").Trim('/');

        if (string.IsNullOrWhiteSpace(endpointUrl) && string.IsNullOrWhiteSpace(accountId))
            throw new InvalidOperationException("Storage:R2:AccountId o Storage:R2:EndpointUrl no configurado.");
        if (string.IsNullOrWhiteSpace(accessKeyId))
            throw new InvalidOperationException("Storage:R2:AccessKeyId no configurado.");
        if (string.IsNullOrWhiteSpace(secretAccessKey))
            throw new InvalidOperationException("Storage:R2:SecretAccessKey no configurado.");

        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            if (accountId!.StartsWith("cfat_", StringComparison.OrdinalIgnoreCase) ||
                accountId.Contains('_') ||
                accountId.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                accountId.Contains("r2.cloudflarestorage.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Storage:R2:AccountId debe contener solo el Account ID de Cloudflare, no un token ni una URL. Usa Storage:R2:EndpointUrl para una URL completa.");
            }

            endpointUrl = $"https://{accountId}.r2.cloudflarestorage.com";
        }

        var config = new AmazonS3Config
        {
            ServiceURL = endpointUrl.TrimEnd('/'),
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
        };

        _client = new AmazonS3Client(new BasicAWSCredentials(accessKeyId, secretAccessKey), config);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var storageKey = $"{_keyPrefix}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext}";

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
            InputStream = fileStream,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true,
            UseChunkEncoding = false,
        };

        await _client.PutObjectAsync(request, ct);
        return storageKey;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return;

        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
        }, ct);
    }

    public string GetPublicUrl(string storageKey)
    {
        return $"{_publicBaseUrl}/{storageKey.TrimStart('/')}";
    }
}
