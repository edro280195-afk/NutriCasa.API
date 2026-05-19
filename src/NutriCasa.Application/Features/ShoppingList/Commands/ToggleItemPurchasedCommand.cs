using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.ShoppingList.Commands;

public record ToggleItemPurchasedCommand : IRequest<Result>
{
    public Guid ItemId { get; init; }
}

public class ToggleItemPurchasedCommandHandler : IRequestHandler<ToggleItemPurchasedCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ToggleItemPurchasedCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ToggleItemPurchasedCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var item = await _context.ShoppingListItems
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, ct);

        if (item is null)
            return Result.Failure("Item no encontrado.", "NOT_FOUND");

        item.IsPurchased = !item.IsPurchased;
        item.PurchasedByUserId = item.IsPurchased ? _currentUser.UserId : null;
        item.PurchasedAt = item.IsPurchased ? DateTime.UtcNow : null;

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
