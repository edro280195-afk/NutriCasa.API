using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.WeeklySummaries.Queries.GetWeeklySummaries;

namespace NutriCasa.Api.Controllers;

[Authorize]
public class WeeklySummaryController : BaseApiController
{
    private readonly IMediator _mediator;

    public WeeklySummaryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummaries()
    {
        return HandleResult(await _mediator.Send(new GetWeeklySummariesQuery()));
    }
}
