using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Infrastructure.Services;

public class ModerationService : IModerationService
{
    private readonly IApplicationDbContext _context;

    public ModerationService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool IsClean, string? Reason, string? Severity)> ModerateTextAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (true, null, null);

        var toxicWords = await _context.ToxicWords
            .Where(t => t.IsActive)
            .ToListAsync(ct);

        var normalizedInput = text.ToLowerInvariant();

        foreach (var word in toxicWords)
        {
            if (word.IsRegex && word.Pattern is not null)
            {
                try
                {
                    if (Regex.IsMatch(normalizedInput, word.Pattern, RegexOptions.IgnoreCase))
                        return (false, word.Word, word.Severity.ToString().ToLowerInvariant());
                }
                catch (RegexParseException)
                {
                    continue;
                }
            }
            else if (normalizedInput.Contains(word.NormalizedWord))
            {
                return (false, word.Word, word.Severity.ToString().ToLowerInvariant());
            }
        }

        return (true, null, null);
    }
}
