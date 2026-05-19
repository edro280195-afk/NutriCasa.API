using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.ShoppingList.Commands.GenerateShoppingList;

public record GenerateShoppingListCommand : IRequest<Result>;

public class GenerateShoppingListCommandHandler : IRequestHandler<GenerateShoppingListCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GenerateShoppingListCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(GenerateShoppingListCommand request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var membership = await _context.GroupMemberships
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (membership is null)
            return Result.Failure("No perteneces a ningún grupo.", "NO_GROUP");

        if (membership.Role != Domain.Enums.GroupRole.Owner && membership.Role != Domain.Enums.GroupRole.Admin)
            return Result.Failure("Solo owner o admin pueden generar la lista de compras.", "FORBIDDEN");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = today.AddDays(-daysSinceMonday);
        var weekEnd = weekStart.AddDays(6);

        // Buscar planes activos de todos los miembros del grupo
        var memberIds = await _context.GroupMemberships
            .Where(m => m.GroupId == membership.GroupId && m.LeftAt == null)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        var activePlans = await _context.WeeklyPlans
            .Include(p => p.Meals)
                .ThenInclude(m => m.Recipe)
            .Where(p => memberIds.Contains(p.UserId)
                     && p.StartDate <= weekEnd
                     && p.EndDate >= weekStart
                     && p.IsActive)
            .ToListAsync(ct);

        if (activePlans.Count == 0)
            return Result.Failure("No hay planes activos en el grupo para esta semana.", "NO_PLANS");

        // Consolidar ingredientes de todas las recetas en todos los planes
        var consolidated = new Dictionary<string, (decimal Amount, string Unit, string Category, decimal Cost)>(StringComparer.OrdinalIgnoreCase);

        foreach (var plan in activePlans)
        {
            foreach (var meal in plan.Meals.Where(m => m.Recipe is not null))
            {
                var recipe = meal.Recipe!;
                if (string.IsNullOrEmpty(recipe.Ingredients)) continue;

                try
                {
                    var ingredients = System.Text.Json.JsonSerializer.Deserialize<List<RecipeIngredientJson>>(recipe.Ingredients);
                    if (ingredients is null) continue;

                    foreach (var ing in ingredients)
                    {
                        var key = ing.Name;

                        if (consolidated.TryGetValue(key, out var existing))
                        {
                            consolidated[key] = (existing.Amount + ing.AmountGr, existing.Unit, existing.Category, existing.Cost + (ing.Kcal * 0.01m));
                        }
                        else
                        {
                            consolidated[key] = (ing.AmountGr, "g", ing.Category ?? "General", ing.Kcal * 0.01m);
                        }
                    }
                }
                catch
                {
                    // Si falla el parseo, ignorar ese ingrediente
                }
            }
        }

        if (consolidated.Count == 0)
            return Result.Failure("No se encontraron ingredientes para consolidar.", "NO_INGREDIENTS");

        // Verificar si ya existe una lista para esta semana
        var existingList = await _context.ShoppingLists
            .FirstOrDefaultAsync(l => l.GroupId == membership.GroupId
                                   && l.WeekStartDate == weekStart, ct);

        if (existingList is not null)
        {
            // Reemplazar items
            _context.ShoppingListItems.RemoveRange(
                _context.ShoppingListItems.Where(i => i.ShoppingListId == existingList.Id));
            _context.ShoppingLists.Remove(existingList);
        }

        var shoppingList = new NutriCasa.Domain.Entities.ShoppingList
        {
            Id = Guid.NewGuid(),
            GroupId = membership.GroupId,
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd,
            TotalEstimatedMxn = consolidated.Values.Sum(v => v.Cost),
            GeneratedAt = DateTime.UtcNow,
        };

        _context.ShoppingLists.Add(shoppingList);

        int sortOrder = 0;
        foreach (var item in consolidated)
        {
            _context.ShoppingListItems.Add(new NutriCasa.Domain.Entities.ShoppingListItem
            {
                Id = Guid.NewGuid(),
                ShoppingListId = shoppingList.Id,
                IngredientName = item.Key,
                TotalAmount = Math.Round(item.Value.Amount, 1),
                Unit = item.Value.Unit,
                Category = item.Value.Category,
                EstimatedCostMxn = Math.Round(item.Value.Cost, 2),
                IsPurchased = false,
                SortOrder = sortOrder++,
            });
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    private class RecipeIngredientJson
    {
        public string Name { get; set; } = "";
        public decimal AmountGr { get; set; }
        public string? Category { get; set; }
        public decimal Kcal { get; set; }
        public string UnitLabel { get; set; } = "g";
    }
}
