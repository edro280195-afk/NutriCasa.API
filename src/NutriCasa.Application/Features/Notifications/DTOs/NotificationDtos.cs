namespace NutriCasa.Application.Features.Notifications.DTOs;

public record NotificationDto
{
    public required Guid Id { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public string? DeepLink { get; init; }
    public string? IconUrl { get; init; }
    public string? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReadAt { get; init; }
}

public record UnreadCountDto
{
    public required int Count { get; init; }
}
