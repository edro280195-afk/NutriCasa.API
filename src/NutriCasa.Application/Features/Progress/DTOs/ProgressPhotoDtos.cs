namespace NutriCasa.Application.Features.Progress.DTOs;

public record ProgressPhotoDto
{
    public Guid PhotoId { get; init; }
    public string PhotoUrl { get; init; } = "";
    public string StorageKey { get; init; } = "";
    public string? Angle { get; init; }
    public string Visibility { get; init; } = "Private";
    public string TakenAt { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public long? FileSizeBytes { get; init; }
}

public record UploadPhotoResultDto
{
    public Guid PhotoId { get; init; }
    public string PhotoUrl { get; init; } = "";
    public string StorageKey { get; init; } = "";
    public string TakenAt { get; init; } = "";
}
