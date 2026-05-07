using NutriCasa.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace NutriCasa.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<UserSubscription> UserSubscriptions { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<UserTrialUsed> UserTrialsUsed { get; }
    DbSet<PushSubscription> PushSubscriptions { get; }
    DbSet<PrivacySettings> PrivacySettings { get; }
    DbSet<MedicalProfile> MedicalProfiles { get; }
    DbSet<KetoProfile> KetoProfiles { get; }
    DbSet<DisclaimerVersion> DisclaimerVersions { get; }
    DbSet<TutorRelationship> TutorRelationships { get; }
    DbSet<UserGoal> UserGoals { get; }
    DbSet<BodyMeasurement> BodyMeasurements { get; }
    DbSet<DailyCheckIn> DailyCheckIns { get; }
    DbSet<ProgressPhoto> ProgressPhotos { get; }
    DbSet<UserMilestone> UserMilestones { get; }
    DbSet<Group> Groups { get; }
    DbSet<GroupMembership> GroupMemberships { get; }
    DbSet<GroupPost> GroupPosts { get; }
    DbSet<PostReaction> PostReactions { get; }
    DbSet<PostComment> PostComments { get; }
    DbSet<Challenge> Challenges { get; }
    DbSet<ChallengeParticipant> ChallengeParticipants { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeRating> RecipeRatings { get; }
    DbSet<WeeklyPlan> WeeklyPlans { get; }
    DbSet<WeeklyPlanMeal> WeeklyPlanMeals { get; }
    DbSet<MealLog> MealLogs { get; }
    DbSet<ShoppingList> ShoppingLists { get; }
    DbSet<ShoppingListItem> ShoppingListItems { get; }
    DbSet<BudgetMode> BudgetModes { get; }
    DbSet<IngredientCatalog> IngredientCatalog { get; }
    DbSet<IngredientSubstitution> IngredientSubstitutions { get; }
    DbSet<StoreCategory> StoreCategories { get; }
    DbSet<UserPreferredStore> UserPreferredStores { get; }
    DbSet<AiInteraction> AiInteractions { get; }
    DbSet<WeeklySummary> WeeklySummaries { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<FeatureFlag> FeatureFlags { get; }
    DbSet<SystemThreshold> SystemThresholds { get; }
    DbSet<ToxicWord> ToxicWords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
