namespace NutriCasa.Application.Common.Interfaces;

public interface IModerationService
{
    Task<(bool IsClean, string? Reason, string? Severity)> ModerateTextAsync(string text, CancellationToken ct = default);
}
