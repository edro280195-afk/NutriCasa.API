using NutriCasa.Application.Features.Onboarding.Commands.CompleteStep4BodyType;
using Xunit;

namespace NutriCasa.Application.Tests.Onboarding;

public class CompleteStep4BodyTypeValidatorTests
{
    private readonly CompleteStep4BodyTypeCommandValidator _validator = new();

    [Theory]
    [InlineData("slim")]
    [InlineData("average")]
    [InlineData("athletic")]
    [InlineData("curvy")]
    [InlineData("plus")]
    [InlineData("heavy")]
    [InlineData("notSure")]
    public void Validate_AcceptsKnownBodyTypes(string bodyType)
    {
        var result = _validator.Validate(new CompleteStep4BodyTypeCommand
        {
            BodyType = bodyType,
        });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown")]
    public void Validate_RejectsInvalidBodyTypes(string bodyType)
    {
        var result = _validator.Validate(new CompleteStep4BodyTypeCommand
        {
            BodyType = bodyType,
        });

        Assert.False(result.IsValid);
    }
}
