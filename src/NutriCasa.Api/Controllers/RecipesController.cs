using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.Recipes.Commands.AddFavorite;
using NutriCasa.Application.Features.Recipes.Commands.RemoveFavorite;
using NutriCasa.Application.Features.Recipes.DTOs;
using NutriCasa.Application.Features.Recipes.Queries.GetFavoriteRecipes;

namespace NutriCasa.Api.Controllers;

[Authorize]
public class RecipesController : BaseApiController
{
    private readonly IMediator _mediator;

    public RecipesController(IMediator mediator) => _mediator = mediator;

    [HttpPost("{recipeId}/favorite")]
    public async Task<IActionResult> AddFavorite(Guid recipeId, CancellationToken ct)
    {
        var command = new AddFavoriteRecipeCommand { RecipeId = recipeId };
        return HandleResult(await _mediator.Send(command, ct));
    }

    [HttpDelete("{recipeId}/favorite")]
    public async Task<IActionResult> RemoveFavorite(Guid recipeId, CancellationToken ct)
    {
        var command = new RemoveFavoriteRecipeCommand { RecipeId = recipeId };
        return HandleResult(await _mediator.Send(command, ct));
    }

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites([FromQuery] string? mealType, CancellationToken ct)
    {
        var query = new GetFavoriteRecipesQuery { MealType = mealType };
        return HandleResult(await _mediator.Send(query, ct));
    }
}
