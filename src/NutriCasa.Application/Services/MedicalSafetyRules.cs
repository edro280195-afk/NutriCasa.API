using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Services;

public static class MedicalSafetyRules
{
    public static bool RequiresHumanReview(
        bool hasDiabetes,
        DiabetesType? diabetesType,
        bool isPregnantOrLactating,
        bool hasKidneyIssues,
        bool hasLiverIssues,
        bool hasPancreasIssues,
        bool hasHeartCondition,
        bool hasEatingDisorderHistory)
    {
        return isPregnantOrLactating
            || hasKidneyIssues
            || hasLiverIssues
            || hasPancreasIssues
            || hasHeartCondition
            || hasEatingDisorderHistory
            || (hasDiabetes && diabetesType == DiabetesType.T1);
    }

    public static bool RequiresHumanReview(MedicalProfile profile)
    {
        return RequiresHumanReview(
            profile.HasDiabetes,
            profile.DiabetesType,
            profile.IsPregnantOrLactating,
            profile.HasKidneyIssues,
            profile.HasLiverIssues,
            profile.HasPancreasIssues,
            profile.HasHeartCondition,
            profile.HasEatingDisorderHistory);
    }

    public static bool HasAbsoluteKetoBlock(MedicalProfile profile, int age)
    {
        return age < 18
            || profile.IsPregnantOrLactating
            || profile.HasEatingDisorderHistory;
    }

    public static bool CanAcceptOverride(MedicalProfile profile, int age)
    {
        return age >= 18
            && profile.RequiresHumanReview
            && !profile.IsPregnantOrLactating
            && !profile.HasEatingDisorderHistory;
    }

    public static string GetAbsoluteBlockMessage(MedicalProfile profile, int age)
    {
        if (age < 18)
            return "Necesitas tener al menos 18 años para generar un plan keto en NutriCasa.";

        if (profile.IsPregnantOrLactating)
            return "Por tu seguridad, NutriCasa no genera planes keto durante embarazo o lactancia. Consulta a tu obstetra.";

        if (profile.HasEatingDisorderHistory)
            return "Por tu seguridad, NutriCasa no genera planes keto si hay antecedente de trastorno alimenticio.";

        return "Tu perfil médico requiere revisión antes de generar un plan.";
    }

    public static string[] GetBlockingConditions(
        bool hasDiabetes,
        DiabetesType? diabetesType,
        bool isPregnantOrLactating,
        bool hasKidneyIssues,
        bool hasLiverIssues,
        bool hasPancreasIssues,
        bool hasHeartCondition,
        bool hasEatingDisorderHistory)
    {
        var conditions = new List<string>();

        if (hasDiabetes && diabetesType == DiabetesType.T1) conditions.Add("diabetes_t1");
        if (isPregnantOrLactating) conditions.Add("pregnancy_or_lactating");
        if (hasKidneyIssues) conditions.Add("kidney_issues");
        if (hasLiverIssues) conditions.Add("liver_issues");
        if (hasPancreasIssues) conditions.Add("pancreas_issues");
        if (hasHeartCondition) conditions.Add("heart_condition");
        if (hasEatingDisorderHistory) conditions.Add("eating_disorder_history");

        return conditions.ToArray();
    }
}
