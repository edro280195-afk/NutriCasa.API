namespace NutriCasa.Application.Common.Models;

// ─────────────────────────────────────────────────────────────────────────────
// GEMINI — Request / Response
// ─────────────────────────────────────────────────────────────────────────────

public record GeneratePlanRequest
{
    public required Guid UserId { get; init; }
    public required string UserName { get; init; }
    public required int Age { get; init; }
    public required string Gender { get; init; }
    public required decimal HeightCm { get; init; }
    public required decimal WeightKg { get; init; }
    public decimal? TargetWeightKg { get; init; }
    public required string ActivityLevel { get; init; }
    public required string BudgetModeCode { get; init; }
    public required string BudgetModeRulesJson { get; init; }
    public required int DailyCalories { get; init; }
    public required decimal CarbsGrams { get; init; }
    public required decimal ProteinGrams { get; init; }
    public required decimal FatGrams { get; init; }
    public required string[] Allergies { get; init; }
    public required string[] DislikedIngredients { get; init; }
    public required string[] DietaryRestrictions { get; init; }
    public required string KetoExperienceLevel { get; init; }
    public required bool IsOverridePlan { get; init; }
    public required string GoalType { get; init; }
    public required DateTime WeekStartDate { get; init; }
    public required string[] PreviousWeekRecipeCodes { get; init; }
    public string? FamilyContext { get; init; }
}

public record GeneratePlanResponse
{
    public required PlanMetadata PlanMetadata { get; init; }
    public required MacrosTarget MacrosTarget { get; init; }
    public required List<DayPlan> Days { get; init; }
    public required List<ShoppingListConsolidatedItem> ShoppingListConsolidated { get; init; }

    // Raw JSON de Gemini para almacenar en original_menu_content
    public string RawJson { get; init; } = string.Empty;
}

public record PlanMetadata
{
    public required string ModeCode { get; init; }
    public decimal EstimatedTotalCostMxn { get; init; }
    public decimal EstimatedCostPerPersonMxn { get; init; }
    public decimal EstimatedCostGourmetBaselineMxn { get; init; }
    public decimal SavingsVsGourmetMxn { get; init; }
    public decimal SavingsVsGourmetPercent { get; init; }
    public string LanguageToneUsed { get; init; } = string.Empty;
    public string ValidationNotes { get; init; } = string.Empty;
}

public record MacrosTarget
{
    public int DailyCalories { get; init; }
    public decimal DailyProteinG { get; init; }
    public decimal DailyFatG { get; init; }
    public decimal DailyCarbsG { get; init; }
}

public record DayPlan
{
    public int DayNumber { get; init; }
    public required string DayName { get; init; }
    public required List<MealPlan> Meals { get; init; }
    public required DayTotals DayTotals { get; init; }
}

public record MealPlan
{
    public required string MealType { get; init; }       // breakfast | lunch | dinner | snack
    public required string RecipeName { get; init; }
    public required List<RecipeIngredient> Ingredients { get; init; }
    public required string Instructions { get; init; }
    public int PrepTimeMin { get; init; }
    public int CookTimeMin { get; init; }
    public int Servings { get; init; } = 1;
    public int TotalCalories { get; init; }
    public decimal TotalProteinG { get; init; }
    public decimal TotalFatG { get; init; }
    public decimal TotalCarbsG { get; init; }
    public decimal EstimatedCostMxn { get; init; }
    public string[] Tags { get; init; } = [];
    public string PrimaryStore { get; init; } = string.Empty;
}

public record RecipeIngredient
{
    public string IngredientCode { get; init; } = string.Empty;
    public required string Name { get; init; }
    public decimal AmountGr { get; init; }
    public string UnitLabel { get; init; } = string.Empty;
    public decimal Kcal { get; init; }
    public decimal ProteinG { get; init; }
    public decimal FatG { get; init; }
    public decimal CarbsG { get; init; }
}

public record DayTotals
{
    public int Calories { get; init; }
    public decimal ProteinG { get; init; }
    public decimal FatG { get; init; }
    public decimal CarbsG { get; init; }
    public decimal EstimatedCostMxn { get; init; }
}

public record ShoppingListConsolidatedItem
{
    public string IngredientCode { get; init; } = string.Empty;
    public required string Name { get; init; }
    public decimal TotalAmountGr { get; init; }
    public string UnitLabel { get; init; } = string.Empty;
    public string StoreCategory { get; init; } = string.Empty;
    public decimal EstimatedCostMxn { get; init; }
    public string Category { get; init; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────────────────────
// SWAP MEAL
// ─────────────────────────────────────────────────────────────────────────────

public record SwapMealRequest
{
    public required Guid UserId { get; init; }
    public required Guid PlanMealId { get; init; }
    public required string CurrentRecipeName { get; init; }
    public required string MealType { get; init; }
    public required string BudgetModeCode { get; init; }
    public required string[] Allergies { get; init; }
    public required string[] DislikedIngredients { get; init; }
    public required int DailyCaloriesTarget { get; init; }
    public required decimal ProteinTarget { get; init; }
    public required decimal FatTarget { get; init; }
    public required decimal CarbsTarget { get; init; }
    public string? SwapReason { get; init; }
}

public record SwapMealResponse
{
    public required MealPlan NewMeal { get; init; }
    public string RawJson { get; init; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────────────────────
// PLAN VALIDATOR
// ─────────────────────────────────────────────────────────────────────────────

public record PlanValidationContext
{
    public required string[] Allergies { get; init; }
    public required string[] DislikedIngredients { get; init; }
    public required string[] DietaryRestrictions { get; init; }
    public required int DailyCaloriesTarget { get; init; }
    public required decimal ProteinTarget { get; init; }
    public required decimal FatTarget { get; init; }
    public required decimal CarbsTarget { get; init; }
    public required int MaxCarbsGrams { get; init; }         // 50 estándar, 60 con override
    public required decimal BmrKcal { get; init; }
    public required decimal TdeeKcal { get; init; }
    public required bool IsOverridePlan { get; init; }
    public required string BudgetModeCode { get; init; }
    public required decimal WeightKg { get; init; }
    public required decimal MinProteinPerKg { get; init; }   // de system_thresholds
}

public record PlanValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public string? CorrectionPrompt { get; init; }
    public List<string> Warnings { get; init; } = [];
}

// ─────────────────────────────────────────────────────────────────────────────
// COST ESTIMATION
// ─────────────────────────────────────────────────────────────────────────────

public record PlanCostEstimate
{
    public decimal TotalCostMxn { get; init; }
    public decimal CostPerPersonMxn { get; init; }
    public decimal GourmetBaselineCostMxn { get; init; }
    public decimal SavingsVsGourmetMxn { get; init; }
    public decimal SavingsVsGourmetPercent { get; init; }
    public Dictionary<string, decimal> CostByCategory { get; init; } = new();
    public Dictionary<string, decimal> CostByStore { get; init; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
// INGREDIENT SUBSTITUTION
// ─────────────────────────────────────────────────────────────────────────────

public record SubstitutionSuggestion
{
    public required string OriginalIngredientName { get; init; }
    public required string ReplacementIngredientName { get; init; }
    public string ReplacementCode { get; init; } = string.Empty;
    public decimal CostSavingsPercent { get; init; }
    public int QualityLossScore { get; init; }
    public string? Notes { get; init; }
}

// ─────────────────────────────────────────────────────────────────────────────
// KETO PROFILE CALCULATOR OUTPUT
// ─────────────────────────────────────────────────────────────────────────────

public record KetoProfileResult
{
    public decimal BmrKcal { get; init; }
    public decimal TdeeKcal { get; init; }
    public int DailyCalories { get; init; }
    public decimal CarbsGrams { get; init; }
    public decimal ProteinGrams { get; init; }
    public decimal FatGrams { get; init; }
    public decimal CarbsPercent { get; init; }
    public decimal ProteinPercent { get; init; }
    public decimal FatPercent { get; init; }
}
