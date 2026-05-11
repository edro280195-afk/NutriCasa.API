namespace NutriCasa.Application.Features.PushSubscriptions.DTOs;

public record SubscribeRequestDto
{
    public required string Endpoint { get; init; }
    public required string P256dhKey { get; init; }
    public required string AuthKey { get; init; }
}

public record UnsubscribeRequestDto
{
    public required string Endpoint { get; init; }
}

public record PushSubscriptionDto
{
    public required Guid Id { get; init; }
    public required string Endpoint { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
}
