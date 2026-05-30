using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Application.Common.Helpers;

public static class GroupTreeHelper
{
    public static async Task<List<Guid>> GetFamilyTreeGroupIdsAsync(IApplicationDbContext context, Guid groupId, CancellationToken ct)
    {
        // 1. Encontrar el root del árbol de grupos
        Guid currentId = groupId;
        while (true)
        {
            var parentId = await context.Groups
                .AsNoTracking()
                .Where(g => g.Id == currentId)
                .Select(g => g.ParentGroupId)
                .FirstOrDefaultAsync(ct);

            if (parentId is null) break;
            currentId = parentId.Value;
        }
        Guid rootGroupId = currentId;

        // 2. Encontrar todos los subgrupos (soporta hasta 3 niveles de profundidad)
        var level1Ids = await context.Groups
            .AsNoTracking()
            .Where(g => g.ParentGroupId == rootGroupId)
            .Select(g => g.Id)
            .ToListAsync(ct);

        var level2Ids = new List<Guid>();
        if (level1Ids.Count > 0)
        {
            level2Ids = await context.Groups
                .AsNoTracking()
                .Where(g => g.ParentGroupId != null && level1Ids.Contains(g.ParentGroupId.Value))
                .Select(g => g.Id)
                .ToListAsync(ct);
        }

        var allFamilyGroupIds = new List<Guid> { rootGroupId };
        allFamilyGroupIds.AddRange(level1Ids);
        allFamilyGroupIds.AddRange(level2Ids);

        return allFamilyGroupIds;
    }
}
