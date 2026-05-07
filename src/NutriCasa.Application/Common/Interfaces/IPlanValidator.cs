namespace NutriCasa.Application.Common.Interfaces;

public interface IPlanValidator
{
    Task<bool> ValidateAsync(string planJson, string modeCode, CancellationToken ct = default);
}
