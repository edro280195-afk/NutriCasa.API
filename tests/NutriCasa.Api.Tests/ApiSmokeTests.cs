using Xunit;

namespace NutriCasa.Api.Tests;

public class ApiSmokeTests
{
    [Fact]
    public void Program_Partial_Class_Exists()
    {
        // Verificar que la clase parcial Program existe para tests de integración futuros
        var type = typeof(NutriCasa.Api.Program);
        Assert.NotNull(type);
    }
}
