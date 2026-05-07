using NutriCasa.Domain.Common;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;
using Xunit;

namespace NutriCasa.Domain.Tests;

public class DomainSmokeTests
{
    [Fact]
    public void User_Inherits_SoftDeletableEntity()
    {
        var user = new User();
        Assert.IsAssignableFrom<SoftDeletableEntity>(user);
        Assert.IsAssignableFrom<AuditableEntity>(user);
        Assert.IsAssignableFrom<BaseEntity>(user);
    }

    [Fact]
    public void User_Default_Values_Are_Correct()
    {
        var user = new User();
        Assert.Equal("America/Mexico_City", user.Timezone);
        Assert.Equal("es-MX", user.PreferredLanguage);
        Assert.Equal(NutritionTrack.Keto, user.NutritionTrack);
        Assert.False(user.IsMinor);
        Assert.Equal(0, user.FailedLoginAttempts);
    }

    [Fact]
    public void All_Enums_Have_Expected_Values()
    {
        Assert.Equal(4, Enum.GetValues<Gender>().Length);
        Assert.Equal(5, Enum.GetValues<ActivityLevel>().Length);
        Assert.Equal(5, Enum.GetValues<GoalType>().Length);
        Assert.Equal(4, Enum.GetValues<MealType>().Length);
        Assert.Equal(6, Enum.GetValues<ChallengeGoalType>().Length);
        Assert.Equal(10, Enum.GetValues<AiInteractionType>().Length);
    }

    [Fact]
    public void BudgetMode_Rules_Default_To_Empty_Json()
    {
        var mode = new BudgetMode();
        Assert.Equal("{}", mode.Rules);
    }

    [Fact]
    public void Recipe_Default_Values()
    {
        var recipe = new Recipe();
        Assert.Equal("[]", recipe.Ingredients);
        Assert.Equal(RecipeSource.AiGenerated, recipe.Source);
        Assert.Equal(1, recipe.Servings);
        Assert.True(recipe.IsPublic);
    }
}
