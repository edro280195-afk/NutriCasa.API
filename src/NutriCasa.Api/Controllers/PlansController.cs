using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.Meals.Commands.LogMeal;
using NutriCasa.Application.Features.Meals.DTOs;
using NutriCasa.Application.Features.Meals.Queries.GetMealLogs;
using NutriCasa.Application.Features.Plans.Commands.AdjustPortion;
using NutriCasa.Application.Features.Plans.Commands.GeneratePlan;
using NutriCasa.Application.Features.Plans.Commands.ReorderMeals;
using NutriCasa.Application.Features.Plans.Commands.SwapMeal;
using NutriCasa.Application.Features.Plans.Commands.ToggleLock;
using NutriCasa.Application.Features.Plans.DTOs;
using NutriCasa.Application.Features.Plans.Queries.GetCurrentPlan;

namespace NutriCasa.Api.Controllers;

[Authorize]
public class PlansController : BaseApiController
{
    private readonly IMediator _mediator;

    public PlansController(IMediator mediator) => _mediator = mediator;

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GeneratePlanRequestDto request, CancellationToken ct)
    {
        var command = new GeneratePlanCommand { WeekStartDate = request.WeekStartDate, ForceRegenerate = request.ForceRegenerate };
        return HandleResult(await _mediator.Send(command, ct));
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
        => HandleResult(await _mediator.Send(new GetCurrentPlanQuery(), ct));

    // ── Adherencia ──

    [HttpPost("{planId}/meals/{planMealId}/log")]
    public async Task<IActionResult> LogMeal(Guid planId, Guid planMealId, [FromBody] LogMealRequestDto request, CancellationToken ct)
    {
        var command = new LogMealCommand { PlanMealId = planMealId, Status = request.Status, SubstitutionNote = request.SubstitutionNote, ActualPortion = request.ActualPortion };
        return HandleResult(await _mediator.Send(command, ct));
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken ct)
        => HandleResult(await _mediator.Send(new GetMealLogsQuery { DateFrom = dateFrom, DateTo = dateTo }, ct));

    // ── Editor drag-and-drop ──

    [HttpPatch("{planId}/meals/reorder")]
    public async Task<IActionResult> ReorderMeals(Guid planId, [FromBody] ReorderMealsRequestDto request, CancellationToken ct)
    {
        var command = new ReorderMealsCommand
        {
            PlanId = planId,
            Moves = request.Moves.Select(m => new MealMove
            {
                PlanMealId = m.PlanMealId,
                NewDayOfWeek = m.NewDayOfWeek,
                NewMealType = m.NewMealType,
                RowVersion = m.RowVersion,
                NewSortOrder = m.NewSortOrder,
            }).ToList(),
        };
        return HandleResult(await _mediator.Send(command, ct));
    }

    [HttpPatch("{planId}/meals/{planMealId}/lock")]
    public async Task<IActionResult> ToggleLock(Guid planId, Guid planMealId, [FromBody] ToggleLockRequestDto request, CancellationToken ct)
    {
        var command = new ToggleLockCommand { PlanMealId = planMealId, Locked = request.Locked };
        return HandleResult(await _mediator.Send(command, ct));
    }

    [HttpPost("{planId}/meals/{planMealId}/swap")]
    public async Task<IActionResult> SwapMeal(Guid planId, Guid planMealId, [FromBody] SwapMealRequestDto request, CancellationToken ct)
    {
        var command = new SwapMealCommand { PlanMealId = planMealId, SwapReason = request.Reason };
        return HandleResult(await _mediator.Send(command, ct));
    }

    [HttpPatch("{planId}/meals/{planMealId}/portion")]
    public async Task<IActionResult> AdjustPortion(Guid planId, Guid planMealId, [FromBody] AdjustPortionRequestDto request, CancellationToken ct)
    {
        var command = new AdjustPortionCommand { PlanMealId = planMealId, PortionMultiplier = request.PortionMultiplier };
        return HandleResult(await _mediator.Send(command, ct));
    }
}
