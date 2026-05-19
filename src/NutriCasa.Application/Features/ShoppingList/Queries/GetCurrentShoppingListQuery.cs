using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.ShoppingList.Queries;

public record GetCurrentShoppingListQuery : IRequest<Result<ShoppingListResultDto>>;

public record ShoppingListResultDto
{
    public Guid ShoppingListId { get; init; }
    public DateOnly WeekStart { get; init; }
    public DateOnly WeekEnd { get; init; }
    public decimal? TotalCost { get; init; }
    public List<ShoppingListItemDto> Items { get; init; } = [];
}

public record ShoppingListItemDto
{
    public Guid ItemId { get; init; }
    public string IngredientName { get; init; } = "";
    public decimal TotalAmount { get; init; }
    public string Unit { get; init; } = "";
    public string? Category { get; init; }
    public decimal? EstimatedCostMxn { get; init; }
    public bool IsPurchased { get; init; }
    public Guid? PurchasedByUserId { get; init; }
    public int SortOrder { get; init; }
}

public class GetCurrentShoppingListQueryHandler : IRequestHandler<GetCurrentShoppingListQuery, Result<ShoppingListResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentShoppingListQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ShoppingListResultDto>> Handle(GetCurrentShoppingListQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<ShoppingListResultDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var membership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == _currentUser.UserId.Value && m.LeftAt == null, ct);

        if (membership is null)
            return Result<ShoppingListResultDto>.Failure("No perteneces a ningún grupo.", "NO_GROUP");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var shoppingList = await _context.ShoppingLists
            .Include(sl => sl.Items.OrderBy(i => i.SortOrder))
            .Where(sl => sl.GroupId == membership.GroupId
                      && sl.WeekStartDate <= today
                      && sl.WeekEndDate >= today)
            .OrderByDescending(sl => sl.GeneratedAt)
            .FirstOrDefaultAsync(ct);

        if (shoppingList is null)
            return Result<ShoppingListResultDto>.Success(new ShoppingListResultDto
            {
                Items = [],
            });

        return Result<ShoppingListResultDto>.Success(new ShoppingListResultDto
        {
            ShoppingListId = shoppingList.Id,
            WeekStart = shoppingList.WeekStartDate,
            WeekEnd = shoppingList.WeekEndDate,
            TotalCost = shoppingList.TotalEstimatedCostMxn ?? shoppingList.TotalEstimatedMxn,
            Items = shoppingList.Items.Select(i => new ShoppingListItemDto
            {
                ItemId = i.Id,
                IngredientName = i.IngredientName,
                TotalAmount = i.TotalAmount,
                Unit = i.Unit,
                Category = i.Category,
                EstimatedCostMxn = i.EstimatedCostMxn,
                IsPurchased = i.IsPurchased,
                PurchasedByUserId = i.PurchasedByUserId,
                SortOrder = i.SortOrder,
            }).ToList(),
        });
    }
}
