using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.Family.Queries;

namespace NutriCasa.Api.Controllers;

public class FamilyController : BaseApiController
{
    private readonly IMediator _mediator;

    public FamilyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("members")]
    [Authorize]
    public async Task<IActionResult> GetMembers(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFamilyMembersQuery(), ct);
        return HandleResult(result);
    }

    [HttpGet("feed")]
    [Authorize]
    public async Task<IActionResult> GetFeed(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFamilyFeedQuery(), ct);
        return HandleResult(result);
    }

    [HttpGet("stats")]
    [Authorize]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFamilyStatsQuery(), ct);
        return HandleResult(result);
    }
}
