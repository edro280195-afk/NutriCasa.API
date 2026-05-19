using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.ShoppingList.Commands;
using NutriCasa.Application.Features.ShoppingList.Commands.GenerateShoppingList;
using NutriCasa.Application.Features.ShoppingList.Queries;

namespace NutriCasa.Api.Controllers;

public class ShoppingListController : BaseApiController
{
    private readonly IMediator _mediator;

    public ShoppingListController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("current")]
    [Authorize]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCurrentShoppingListQuery(), ct);
        return HandleResult(result);
    }

    [HttpPost("generate")]
    [Authorize]
    public async Task<IActionResult> Generate(CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateShoppingListCommand(), ct);
        return HandleResult(result);
    }

    [HttpPatch("items/{itemId:guid}/toggle-purchased")]
    [Authorize]
    public async Task<IActionResult> TogglePurchased(Guid itemId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ToggleItemPurchasedCommand { ItemId = itemId }, ct);
        return HandleResult(result);
    }
}
