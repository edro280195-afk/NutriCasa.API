using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.Plans.Commands.GeneratePlan;
using NutriCasa.Application.Features.Plans.DTOs;
using NutriCasa.Application.Features.Plans.Queries.GetCurrentPlan;

namespace NutriCasa.Api.Controllers;

[Authorize]
public class PlansController : BaseApiController
{
    private readonly IMediator _mediator;

    public PlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GeneratePlanRequestDto request, CancellationToken ct)
    {
        var command = new GeneratePlanCommand
        {
            WeekStartDate = request.WeekStartDate,
            ForceRegenerate = request.ForceRegenerate
        };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var query = new GetCurrentPlanQuery();
        var result = await _mediator.Send(query, ct);
        return HandleResult(result);
    }
}
