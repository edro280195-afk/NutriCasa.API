namespace NutriCasa.Application.Features.Auth.DTOs;

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public record RegisterUserRequest
{
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required DateOnly BirthDate { get; init; }
}

public record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}

public record ForgotPasswordRequest
{
    public required string Email { get; init; }
}

public record ResetPasswordRequest
{
    public required string Token { get; init; }
    public required string NewPassword { get; init; }
}

public record LogoutRequest
{
    public required string RefreshToken { get; init; }
}

// ─── Response DTOs ────────────────────────────────────────────────────────────

public record RegisterResponse
{
    public string Message { get; init; } = "Revisa tu correo para verificar tu cuenta";
}

public record AuthTokenResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required int ExpiresIn { get; init; }       // en segundos
    public required UserSummaryDto User { get; init; }
}

public record UserSummaryDto
{
    public required Guid UserId { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required bool EmailVerified { get; init; }
    public required bool OnboardingComplete { get; init; }
}

public record CurrentUserResponse
{
    public required Guid UserId { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required bool EmailVerified { get; init; }
    public required bool OnboardingComplete { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public string? Gender { get; init; }
    public DateOnly? BirthDate { get; init; }
    public decimal? HeightCm { get; init; }
    public DateTime? LastLoginAt { get; init; }
}
