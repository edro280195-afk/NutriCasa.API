using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.Recipes.Commands.RemoveFavorite;

public record RemoveFavoriteRecipeCommand : IRequest<Result>
{
    public required Guid RecipeId { get; init; }
}

public class RemoveFavoriteRecipeCommandHandler : IRequestHandler<RemoveFavoriteRecipeCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveFavoriteRecipeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(RemoveFavoriteRecipeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var favorite = await _context.FavoriteRecipes
            .FirstOrDefaultAsync(f => f.UserId == userId && f.RecipeId == request.RecipeId, cancellationToken);

        if (favorite is null)
            return Result.Success();

        _context.FavoriteRecipes.Remove(favorite);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
