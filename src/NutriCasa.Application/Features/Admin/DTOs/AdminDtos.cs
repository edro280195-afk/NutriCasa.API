namespace NutriCasa.Application.Features.Admin.DTOs;

public record AdminDashboardDto
{
    public required int TotalUsers { get; init; }
    public required int ActiveSubscriptions { get; init; }
    public required int PostsToday { get; init; }
    public required int NewUsersToday { get; init; }
    public required int TotalRecipes { get; init; }
    public required int PendingVerifications { get; init; }
    public List<RecentPostDto>? RecentPosts { get; init; }
}

public record RecentPostDto
{
    public required Guid PostId { get; init; }
    public required string Content { get; init; }
    public required string AuthorName { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public record AdminUserDto
{
    public required Guid UserId { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public bool EmailVerified { get; init; }
    public bool OnboardingComplete { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public record AdminPostDto
{
    public required Guid PostId { get; init; }
    public required string Content { get; init; }
    public required string AuthorName { get; init; }
    public required string GroupName { get; init; }
    public int ReactionCount { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public record UpdateUserRoleRequestDto
{
    public required string Role { get; init; }
}
