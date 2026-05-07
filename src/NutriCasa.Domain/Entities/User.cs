using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

/// <summary>
/// Usuario de NutriCasa. Tabla principal del sistema.
/// </summary>
public class User : SoftDeletableEntity
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateOnly BirthDate { get; set; }
    public Gender Gender { get; set; }
    public decimal HeightCm { get; set; }
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.Sedentary;
    public string? BodyTypeSelected { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string Timezone { get; set; } = "America/Mexico_City";
    public string PreferredLanguage { get; set; } = "es-MX";
    public NutritionTrack NutritionTrack { get; set; } = NutritionTrack.Keto;

    // Soporte de menores (V2)
    public bool IsMinor { get; set; }
    public Guid? TutorUserId { get; set; }
    public DateTime? TutorConsentAt { get; set; }
    public Guid? TutorConsentVersionId { get; set; }

    // Verificación y disclaimer
    public DateTime? EmailVerifiedAt { get; set; }
    public string? EmailVerificationToken { get; set; }
    public DateTime? DisclaimerAcceptedAt { get; set; }
    public Guid? DisclaimerVersionId { get; set; }

    // Sesión y seguridad
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }

    // Borrado con gracia
    public DateTime? DeletionRequestedAt { get; set; }
    public DateTime? DeletionScheduledFor { get; set; }
    public DateTime? DeletionCancelledAt { get; set; }

    // Budget mode (del delta 002)
    public Guid? BudgetModeId { get; set; }
    public DateTime? BudgetModeChangedAt { get; set; }

    // Navegación
    public User? TutorUser { get; set; }
    public DisclaimerVersion? TutorConsentVersion { get; set; }
    public DisclaimerVersion? DisclaimerVersion { get; set; }
    public BudgetMode? BudgetMode { get; set; }
    public MedicalProfile? MedicalProfile { get; set; }
    public KetoProfile? KetoProfile { get; set; }
    public PrivacySettings? PrivacySettings { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    public ICollection<UserGoal> UserGoals { get; set; } = new List<UserGoal>();
    public ICollection<GroupMembership> GroupMemberships { get; set; } = new List<GroupMembership>();
}
