using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Domain.Entities;
using NutriCasa.Infrastructure.Persistence.Interceptors;

namespace NutriCasa.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly AuditableEntityInterceptor _auditableInterceptor;
    private readonly SoftDeleteInterceptor _softDeleteInterceptor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditableEntityInterceptor auditableInterceptor,
        SoftDeleteInterceptor softDeleteInterceptor) : base(options)
    {
        _auditableInterceptor = auditableInterceptor;
        _softDeleteInterceptor = softDeleteInterceptor;
    }

    // Identidad y autenticación
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<UserTrialUsed> UserTrialsUsed => Set<UserTrialUsed>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<PrivacySettings> PrivacySettings => Set<PrivacySettings>();

    // Salud y disclaimers
    public DbSet<MedicalProfile> MedicalProfiles => Set<MedicalProfile>();
    public DbSet<KetoProfile> KetoProfiles => Set<KetoProfile>();
    public DbSet<DisclaimerVersion> DisclaimerVersions => Set<DisclaimerVersion>();

    // Tutores
    public DbSet<TutorRelationship> TutorRelationships => Set<TutorRelationship>();

    // Metas y métricas
    public DbSet<UserGoal> UserGoals => Set<UserGoal>();
    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();
    public DbSet<DailyCheckIn> DailyCheckIns => Set<DailyCheckIn>();
    public DbSet<ProgressPhoto> ProgressPhotos => Set<ProgressPhoto>();
    public DbSet<UserMilestone> UserMilestones => Set<UserMilestone>();

    // Grupos y social
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();
    public DbSet<GroupPost> GroupPosts => Set<GroupPost>();
    public DbSet<PostReaction> PostReactions => Set<PostReaction>();
    public DbSet<PostComment> PostComments => Set<PostComment>();
    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<ChallengeParticipant> ChallengeParticipants => Set<ChallengeParticipant>();

    // Nutrición y planes
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeRating> RecipeRatings => Set<RecipeRating>();
    public DbSet<FavoriteRecipe> FavoriteRecipes => Set<FavoriteRecipe>();
    public DbSet<WeeklyPlan> WeeklyPlans => Set<WeeklyPlan>();
    public DbSet<WeeklyPlanMeal> WeeklyPlanMeals => Set<WeeklyPlanMeal>();
    public DbSet<MealLog> MealLogs => Set<MealLog>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();

    // Modos de presupuesto y catálogo
    public DbSet<BudgetMode> BudgetModes => Set<BudgetMode>();
    public DbSet<IngredientCatalog> IngredientCatalog => Set<IngredientCatalog>();
    public DbSet<IngredientSubstitution> IngredientSubstitutions => Set<IngredientSubstitution>();
    public DbSet<StoreCategory> StoreCategories => Set<StoreCategory>();
    public DbSet<UserPreferredStore> UserPreferredStores => Set<UserPreferredStore>();

    // Sistema
    public DbSet<AiInteraction> AiInteractions => Set<AiInteraction>();
    public DbSet<WeeklySummary> WeeklySummaries => Set<WeeklySummary>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<SystemThreshold> SystemThresholds => Set<SystemThreshold>();
    public DbSet<ToxicWord> ToxicWords => Set<ToxicWord>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableInterceptor, _softDeleteInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
