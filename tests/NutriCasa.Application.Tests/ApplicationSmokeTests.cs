using Microsoft.Extensions.DependencyInjection;
using NutriCasa.Application;
using NutriCasa.Application.Common.Interfaces;
using Xunit;

namespace NutriCasa.Application.Tests;

public class ApplicationSmokeTests
{
    [Fact]
    public void AddApplication_Registers_MediatR()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        var provider = services.BuildServiceProvider();

        // MediatR debería estar registrado
        var mediator = provider.GetService<MediatR.IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void Result_Success_Works()
    {
        var result = Common.Models.Result<string>.Success("ok");
        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Result_Failure_Works()
    {
        var result = Common.Models.Result<string>.Failure("Error message", "ERR_001");
        Assert.False(result.IsSuccess);
        Assert.Equal("Error message", result.Error);
        Assert.Equal("ERR_001", result.ErrorCode);
    }

    [Fact]
    public void ValidationException_Groups_By_Property()
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Email", "El email es requerido"),
            new("Email", "El email no es válido"),
            new("Password", "La contraseña es requerida")
        };

        var ex = new Common.Exceptions.ValidationException(failures);
        Assert.Equal(2, ex.Errors.Count);
        Assert.Equal(2, ex.Errors["Email"].Length);
        Assert.Single(ex.Errors["Password"]);
    }
}
