using NutriCasa.Application.Services;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;
using Xunit;

namespace NutriCasa.Application.Tests.Services;

public class MedicalSafetyRulesTests
{
    // ── RequiresHumanReview (parameter overload) ──

    [Fact]
    public void RequiresHumanReview_SinCondiciones_ReturnsFalse()
    {
        var result = MedicalSafetyRules.RequiresHumanReview(
            hasDiabetes: false, diabetesType: null,
            isPregnantOrLactating: false,
            hasKidneyIssues: false, hasLiverIssues: false,
            hasPancreasIssues: false, hasHeartCondition: false,
            hasEatingDisorderHistory: false);

        Assert.False(result);
    }

    [Fact]
    public void RequiresHumanReview_Embarazo_ReturnsTrue()
    {
        var result = MedicalSafetyRules.RequiresHumanReview(
            false, null, true, false, false, false, false, false);
        Assert.True(result);
    }

    [Fact]
    public void RequiresHumanReview_Rinones_ReturnsTrue()
    {
        var result = MedicalSafetyRules.RequiresHumanReview(
            false, null, false, true, false, false, false, false);
        Assert.True(result);
    }

    [Fact]
    public void RequiresHumanReview_Higado_ReturnsTrue()
    {
        var result = MedicalSafetyRules.RequiresHumanReview(
            false, null, false, false, true, false, false, false);
        Assert.True(result);
    }

    [Fact]
    public void RequiresHumanReview_Pancreas_ReturnsTrue()
    {
        var result = MedicalSafetyRules.RequiresHumanReview(
            false, null, false, false, false, true, false, false);
        Assert.True(result);
    }

    [Fact]
    public void RequiresHumanReview_Corazon_ReturnsTrue()
    {
        var result = MedicalSafetyRules.RequiresHumanReview(
            false, null, false, false, false, false, true, false);
        Assert.True(result);
    }

    [Fact]
    public void RequiresHumanReview_TCA_ReturnsTrue()
    {
        var result = MedicalSafetyRules.RequiresHumanReview(
            false, null, false, false, false, false, false, true);
        Assert.True(result);
    }

    [Fact]
    public void RequiresHumanReview_DiabetesT1_ReturnsTrue()
    {
        var result = MedicalSafetyRules.RequiresHumanReview(
            true, DiabetesType.T1, false, false, false, false, false, false);
        Assert.True(result);
    }

    [Fact]
    public void RequiresHumanReview_DiabetesT2_ReturnsFalse()
    {
        var result = MedicalSafetyRules.RequiresHumanReview(
            true, DiabetesType.T2, false, false, false, false, false, false);
        Assert.False(result);
    }

    [Fact]
    public void RequiresHumanReview_DiabetesGestacional_ReturnsFalse()
    {
        var result = MedicalSafetyRules.RequiresHumanReview(
            true, DiabetesType.Gestational, false, false, false, false, false, false);
        Assert.False(result);
    }

    [Fact]
    public void RequiresHumanReview_MultiplesCondiciones_ReturnsTrue()
    {
        var result = MedicalSafetyRules.RequiresHumanReview(
            true, DiabetesType.T1, true, true, true, true, true, true);
        Assert.True(result);
    }

    // ── RequiresHumanReview (MedicalProfile overload) ──

    [Fact]
    public void RequiresHumanReview_ProfileSinCondiciones_ReturnsFalse()
    {
        var profile = CreateProfile();
        Assert.False(MedicalSafetyRules.RequiresHumanReview(profile));
    }

    [Fact]
    public void RequiresHumanReview_ProfileConEmbarazo_ReturnsTrue()
    {
        var profile = CreateProfile(isPregnantOrLactating: true);
        Assert.True(MedicalSafetyRules.RequiresHumanReview(profile));
    }

    [Fact]
    public void RequiresHumanReview_ProfileConDiabetesT1_ReturnsTrue()
    {
        var profile = CreateProfile(hasDiabetes: true, diabetesType: DiabetesType.T1);
        Assert.True(MedicalSafetyRules.RequiresHumanReview(profile));
    }

    [Fact]
    public void RequiresHumanReview_ProfileConDiabetesT2_ReturnsFalse()
    {
        var profile = CreateProfile(hasDiabetes: true, diabetesType: DiabetesType.T2);
        Assert.False(MedicalSafetyRules.RequiresHumanReview(profile));
    }

    // ── HasAbsoluteKetoBlock ──

    [Fact]
    public void HasAbsoluteKetoBlock_MenorDeEdad_ReturnsTrue()
    {
        var profile = CreateProfile();
        Assert.True(MedicalSafetyRules.HasAbsoluteKetoBlock(profile, 17));
    }

    [Fact]
    public void HasAbsoluteKetoBlock_MayorDeEdadSinCondiciones_ReturnsFalse()
    {
        var profile = CreateProfile();
        Assert.False(MedicalSafetyRules.HasAbsoluteKetoBlock(profile, 18));
    }

    [Fact]
    public void HasAbsoluteKetoBlock_EmbarazoMayor_ReturnsTrue()
    {
        var profile = CreateProfile(isPregnantOrLactating: true);
        Assert.True(MedicalSafetyRules.HasAbsoluteKetoBlock(profile, 25));
    }

    [Fact]
    public void HasAbsoluteKetoBlock_TCA_ReturnsTrue()
    {
        var profile = CreateProfile(hasEatingDisorderHistory: true);
        Assert.True(MedicalSafetyRules.HasAbsoluteKetoBlock(profile, 30));
    }

    [Fact]
    public void HasAbsoluteKetoBlock_MayorConRinones_ReturnsFalse()
    {
        var profile = CreateProfile(hasKidneyIssues: true);
        Assert.False(MedicalSafetyRules.HasAbsoluteKetoBlock(profile, 18));
    }

    [Fact]
    public void HasAbsoluteKetoBlock_EdadLimite18_ReturnsFalse()
    {
        var profile = CreateProfile();
        Assert.False(MedicalSafetyRules.HasAbsoluteKetoBlock(profile, 18));
    }

    [Fact]
    public void HasAbsoluteKetoBlock_Edad0_ReturnsTrue()
    {
        var profile = CreateProfile();
        Assert.True(MedicalSafetyRules.HasAbsoluteKetoBlock(profile, 0));
    }

    // ── CanAcceptOverride ──

    [Fact]
    public void CanAcceptOverride_MayorSinCondiciones_ReturnsFalse()
    {
        var profile = CreateProfile();
        profile.RequiresHumanReview = false;
        Assert.False(MedicalSafetyRules.CanAcceptOverride(profile, 25));
    }

    [Fact]
    public void CanAcceptOverride_MayorConRinones_ReturnsTrue()
    {
        var profile = CreateProfile(hasKidneyIssues: true);
        Assert.True(MedicalSafetyRules.CanAcceptOverride(profile, 25));
    }

    [Fact]
    public void CanAcceptOverride_MenorDeEdad_ReturnsFalse()
    {
        var profile = CreateProfile(hasKidneyIssues: true);
        Assert.False(MedicalSafetyRules.CanAcceptOverride(profile, 17));
    }

    [Fact]
    public void CanAcceptOverride_EmbarazoConOverride_ReturnsFalse()
    {
        var profile = CreateProfile(isPregnantOrLactating: true);
        Assert.False(MedicalSafetyRules.CanAcceptOverride(profile, 25));
    }

    [Fact]
    public void CanAcceptOverride_TCAConOverride_ReturnsFalse()
    {
        var profile = CreateProfile(hasEatingDisorderHistory: true);
        Assert.False(MedicalSafetyRules.CanAcceptOverride(profile, 25));
    }

    [Fact]
    public void CanAcceptOverride_SinRequiresReview_ReturnsFalse()
    {
        var profile = CreateProfile();
        profile.RequiresHumanReview = false;
        Assert.False(MedicalSafetyRules.CanAcceptOverride(profile, 30));
    }

    // ── GetAbsoluteBlockMessage ──

    [Fact]
    public void GetAbsoluteBlockMessage_Menor_ReturnsEdadMessage()
    {
        var profile = CreateProfile();
        var msg = MedicalSafetyRules.GetAbsoluteBlockMessage(profile, 16);
        Assert.Contains("18 años", msg);
    }

    [Fact]
    public void GetAbsoluteBlockMessage_Embarazo_ReturnsEmbarazoMessage()
    {
        var profile = CreateProfile(isPregnantOrLactating: true);
        var msg = MedicalSafetyRules.GetAbsoluteBlockMessage(profile, 25);
        Assert.Contains("embarazo", msg.ToLowerInvariant());
    }

    [Fact]
    public void GetAbsoluteBlockMessage_TCA_ReturnsTCAMessage()
    {
        var profile = CreateProfile(hasEatingDisorderHistory: true);
        var msg = MedicalSafetyRules.GetAbsoluteBlockMessage(profile, 25);
        Assert.Contains("trastorno", msg.ToLowerInvariant());
    }

    [Fact]
    public void GetAbsoluteBlockMessage_NoBlock_ReturnsDefaultMessage()
    {
        var profile = CreateProfile(hasKidneyIssues: true);
        profile.RequiresHumanReview = true;
        var msg = MedicalSafetyRules.GetAbsoluteBlockMessage(profile, 25);
        Assert.Contains("revisión", msg.ToLowerInvariant());
    }

    [Fact]
    public void GetAbsoluteBlockMessage_MenorEmbarazada_PriorizaEdad()
    {
        var profile = CreateProfile(isPregnantOrLactating: true);
        var msg = MedicalSafetyRules.GetAbsoluteBlockMessage(profile, 15);
        Assert.Contains("18 años", msg);
        Assert.DoesNotContain("embarazo", msg.ToLowerInvariant());
    }

    [Fact]
    public void GetAbsoluteBlockMessage_MenorConTCA_PriorizaEdad()
    {
        var profile = CreateProfile(hasEatingDisorderHistory: true);
        var msg = MedicalSafetyRules.GetAbsoluteBlockMessage(profile, 14);
        Assert.Contains("18 años", msg);
    }

    // ── GetBlockingConditions ──

    [Fact]
    public void GetBlockingConditions_SinCondiciones_ReturnsEmpty()
    {
        var result = MedicalSafetyRules.GetBlockingConditions(false, null, false, false, false, false, false, false);
        Assert.Empty(result);
    }

    [Fact]
    public void GetBlockingConditions_DiabetesT1_ReturnsDiabetes()
    {
        var result = MedicalSafetyRules.GetBlockingConditions(true, DiabetesType.T1, false, false, false, false, false, false);
        Assert.Contains("diabetes_t1", result);
    }

    [Fact]
    public void GetBlockingConditions_Embarazo_ReturnsPregnancy()
    {
        var result = MedicalSafetyRules.GetBlockingConditions(false, null, true, false, false, false, false, false);
        Assert.Contains("pregnancy_or_lactating", result);
    }

    [Fact]
    public void GetBlockingConditions_Todas_ReturnsAll()
    {
        var result = MedicalSafetyRules.GetBlockingConditions(true, DiabetesType.T1, true, true, true, true, true, true);
        Assert.Equal(7, result.Length);
        Assert.Contains("diabetes_t1", result);
        Assert.Contains("pregnancy_or_lactating", result);
        Assert.Contains("kidney_issues", result);
        Assert.Contains("liver_issues", result);
        Assert.Contains("pancreas_issues", result);
        Assert.Contains("heart_condition", result);
        Assert.Contains("eating_disorder_history", result);
    }

    [Fact]
    public void GetBlockingConditions_DiabetesT2_SinBlock()
    {
        var result = MedicalSafetyRules.GetBlockingConditions(true, DiabetesType.T2, false, false, false, false, false, false);
        Assert.Empty(result);
    }

    // ── Helpers ──

    private static MedicalProfile CreateProfile(
        bool hasDiabetes = false,
        DiabetesType? diabetesType = null,
        bool isPregnantOrLactating = false,
        bool hasKidneyIssues = false,
        bool hasLiverIssues = false,
        bool hasPancreasIssues = false,
        bool hasHeartCondition = false,
        bool hasEatingDisorderHistory = false)
    {
        return new MedicalProfile
        {
            HasDiabetes = hasDiabetes,
            DiabetesType = diabetesType,
            IsPregnantOrLactating = isPregnantOrLactating,
            HasKidneyIssues = hasKidneyIssues,
            HasLiverIssues = hasLiverIssues,
            HasPancreasIssues = hasPancreasIssues,
            HasHeartCondition = hasHeartCondition,
            HasEatingDisorderHistory = hasEatingDisorderHistory,
            RequiresHumanReview = hasDiabetes && diabetesType == DiabetesType.T1
                               || isPregnantOrLactating
                               || hasKidneyIssues
                               || hasLiverIssues
                               || hasPancreasIssues
                               || hasHeartCondition
                               || hasEatingDisorderHistory,
        };
    }
}
