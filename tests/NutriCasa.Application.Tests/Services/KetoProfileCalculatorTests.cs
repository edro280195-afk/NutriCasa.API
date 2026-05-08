using NutriCasa.Application.Services;
using NutriCasa.Domain.Enums;
using Xunit;

namespace NutriCasa.Application.Tests.Services;

public class KetoProfileCalculatorTests
{
    private readonly KetoProfileCalculator _sut = new();

    [Fact]
    public void Calculate_HombreSedentario_ReturnsCorrectValues()
    {
        var result = _sut.Calculate(
            weightKg: 90m,
            heightCm: 175m,
            age: 35,
            gender: Gender.Male,
            activityLevel: ActivityLevel.Sedentary,
            goalType: GoalType.WeightLoss,
            bmrCalorieFloorFactor: 0.85m,
            tdeeCalorieCeilingFactor: 1.10m,
            minimumProteinPerKg: 0.8m);

        // BMR = (10*90) + (6.25*175) - (5*35) + 5 = 900 + 1093.75 - 175 + 5 = 1823.75
        Assert.InRange(result.BmrKcal, 1823m, 1824m);
        // TDEE = BMR * 1.2 = 2188.5
        Assert.InRange(result.TdeeKcal, 2188m, 2189m);
        // Daily = TDEE * 0.80 = 1750.8, clamp entre BMR*0.85=1550.2 y TDEE*1.10=2407.4
        Assert.InRange(result.DailyCalories, 1500, 1800);
        // Carbs = 5% de 1750.8 / 4 = ~21.9g
        Assert.InRange(result.CarbsGrams, 18, 24);
        // Proteína mínima: 0.8 * 90 = 72g
        Assert.True(result.ProteinGrams >= 72m);
        // Fat = resto
        Assert.InRange(result.FatGrams, 120, 150);
    }

    [Fact]
    public void Calculate_MujerActiva_ReturnsCorrectValues()
    {
        var result = _sut.Calculate(
            weightKg: 65m,
            heightCm: 165m,
            age: 28,
            gender: Gender.Female,
            activityLevel: ActivityLevel.Active,
            goalType: GoalType.WeightLoss,
            bmrCalorieFloorFactor: 0.85m,
            tdeeCalorieCeilingFactor: 1.10m,
            minimumProteinPerKg: 0.8m);

        // BMR = (10*65) + (6.25*165) - (5*28) - 161 = 650 + 1031.25 - 140 - 161 = 1380.25
        Assert.InRange(result.BmrKcal, 1380m, 1381m);
        // TDEE = BMR * 1.725 = 2380.9
        Assert.InRange(result.TdeeKcal, 2380m, 2381m);
        // Proteína mínima: 0.8 * 65 = 52g
        Assert.True(result.ProteinGrams >= 52m);
        // Todos los % deben sumar ~100%
        var total = result.CarbsPercent + result.ProteinPercent + result.FatPercent;
        Assert.InRange(total, 98m, 102m);
    }

    [Fact]
    public void Calculate_ModoAtletico_AplicaProteinaMinimaCorrecta()
    {
        var result = _sut.Calculate(
            weightKg: 80m,
            heightCm: 180m,
            age: 30,
            gender: Gender.Male,
            activityLevel: ActivityLevel.Active,
            goalType: GoalType.WeightLoss,
            bmrCalorieFloorFactor: 0.85m,
            tdeeCalorieCeilingFactor: 1.10m,
            minimumProteinPerKg: 0.8m,
            budgetModeMinProteinPerKg: 1.6m);

        // Modo atlético: proteína mínima = 1.6 * 80 = 128g
        Assert.True(result.ProteinGrams >= 128m,
            $"Proteína ({result.ProteinGrams}g) debe ser >= 128g para modo atlético");
    }

    [Fact]
    public void Calculate_OverrideMedico_RespetaLimiteCarbs()
    {
        var result = _sut.Calculate(
            weightKg: 75m,
            heightCm: 170m,
            age: 40,
            gender: Gender.Female,
            activityLevel: ActivityLevel.Sedentary,
            goalType: GoalType.WeightLoss,
            bmrCalorieFloorFactor: 0.85m,
            tdeeCalorieCeilingFactor: 1.10m,
            minimumProteinPerKg: 0.8m,
            isOverridePlan: true,
            overrideMaxCarbsGrams: 60);

        // Con override, carbs no deben exceder 60g
        Assert.True(result.CarbsGrams <= 60m,
            $"Carbs ({result.CarbsGrams}g) debe ser <= 60g con override médico");
    }

    [Fact]
    public void Calculate_NonBinary_UsaPromedioDeFormulas()
    {
        var femaleBmr = (10m * 70m) + (6.25m * 165m) - (5m * 30m) - 161m;
        var maleBmr = (10m * 70m) + (6.25m * 165m) - (5m * 30m) + 5m;
        var expectedBmr = (femaleBmr + maleBmr) / 2m;

        var result = _sut.Calculate(
            weightKg: 70m,
            heightCm: 165m,
            age: 30,
            gender: Gender.NonBinary,
            activityLevel: ActivityLevel.Moderate,
            goalType: GoalType.Maintenance,
            bmrCalorieFloorFactor: 0.85m,
            tdeeCalorieCeilingFactor: 1.10m,
            minimumProteinPerKg: 0.8m);

        Assert.Equal(expectedBmr, result.BmrKcal, 1);
    }
}
