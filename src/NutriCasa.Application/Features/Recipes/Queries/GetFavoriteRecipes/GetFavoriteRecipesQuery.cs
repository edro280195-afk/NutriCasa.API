using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Recipes.DTOs;

namespace NutriCasa.Application.Features.Recipes.Queries.GetFavoriteRecipes;

public record GetFavoriteRecipesQuery : IRequest<Result<List<FavoriteRecipeDto>>>
{
    public string? MealType { get; init; }
}

public class GetFavoriteRecipesQueryHandler : IRequestHandler<GetFavoriteRecipesQuery, Result<List<FavoriteRecipeDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFavoriteRecipesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<FavoriteRecipeDto>>> Handle(GetFavoriteRecipesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result<List<FavoriteRecipeDto>>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var query = _context.FavoriteRecipes
            .Include(f => f.Recipe)
            .Where(f => f.UserId == userId);

        if (!string.IsNullOrWhiteSpace(request.MealType) &&
            Enum.TryParse<Domain.Enums.MealType>(request.MealType, ignoreCase: true, out var mealType))
        {
            query = query.Where(f => f.Recipe.MealType == mealType);
        }

        var favorites = await query
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FavoriteRecipeDto
            {
                FavoriteId = f.Id,
                RecipeId = f.RecipeId,
                RecipeName = f.Recipe.Name,
                Calories = f.Recipe.BaseCalories,
                ProteinGr = f.Recipe.BaseProteinGr,
                FatGr = f.Recipe.BaseFatGr,
                CarbsGr = f.Recipe.BaseCarbsGr,
                MealType = f.Recipe.MealType.ToString(),
                PhotoUrl = f.Recipe.PhotoUrl,
                Slug = f.Recipe.Slug,
                CreatedAt = f.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Result<List<FavoriteRecipeDto>>.Success(favorites);
    }
}
