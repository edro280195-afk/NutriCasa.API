using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.Progress.Queries;

namespace NutriCasa.Api.Controllers;

public class ProgressController : BaseApiController
{
    private readonly IMediator _mediator;

    public ProgressController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("summary")]
    [Authorize]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProgressSummaryQuery(), ct);
        return HandleResult(result);
    }

    [HttpGet("weight-history")]
    [Authorize]
    public async Task<IActionResult> GetWeightHistory(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWeightHistoryQuery(), ct);
        return HandleResult(result);
    }

    [HttpGet("checkins")]
    [Authorize]
    public async Task<IActionResult> GetCheckins(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCheckinHeatmapQuery(), ct);
        return HandleResult(result);
    }

    [HttpGet("macros/weekly")]
    [Authorize]
    public async Task<IActionResult> GetWeeklyMacros(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWeeklyMacrosQuery(), ct);
        return HandleResult(result);
    }
}
