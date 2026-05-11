using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.Recipes.Commands.AddFavorite;

public record AddFavoriteRecipeCommand : IRequest<Result>
{
    public required Guid RecipeId { get; init; }
}

public class AddFavoriteRecipeCommandHandler : IRequestHandler<AddFavoriteRecipeCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public AddFavoriteRecipeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result> Handle(AddFavoriteRecipeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var recipe = await _context.Recipes
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId, cancellationToken);

        if (recipe is null)
            return Result.Failure("Receta no encontrada.", "NOT_FOUND");

        var alreadyFavorited = await _context.FavoriteRecipes
            .AnyAsync(f => f.UserId == userId && f.RecipeId == request.RecipeId, cancellationToken);

        if (alreadyFavorited)
            return Result.Success();

        var favorite = new FavoriteRecipe
        {
            UserId = userId,
            RecipeId = request.RecipeId,
            CreatedAt = _dateTimeService.UtcNow,
        };

        _context.FavoriteRecipes.Add(favorite);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
