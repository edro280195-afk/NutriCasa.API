using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.Subscriptions.Commands.CancelSubscription;
using NutriCasa.Application.Features.Subscriptions.Commands.CreateSubscription;
using NutriCasa.Application.Features.Subscriptions.DTOs;
using NutriCasa.Application.Features.Subscriptions.Queries.GetMySubscription;
using NutriCasa.Application.Features.Subscriptions.Queries.GetPlans;

namespace NutriCasa.Api.Controllers;

[Authorize]
public class SubscriptionsController : BaseApiController
{
    private readonly IMediator _mediator;

    public SubscriptionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
        => HandleResult(await _mediator.Send(new GetPlansQuery(), ct));

    [HttpGet("my")]
    public async Task<IActionResult> GetMySubscription(CancellationToken ct)
        => HandleResult(await _mediator.Send(new GetMySubscriptionQuery(), ct));

    [HttpPost("create")]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateCheckoutRequestDto request, CancellationToken ct)
    {
        var command = new CreateSubscriptionCommand { PlanId = request.PlanId, IsTrial = false };
        return HandleResult(await _mediator.Send(command, ct));
    }

    [HttpPost("trial")]
    public async Task<IActionResult> StartTrial([FromBody] TrialSubscriptionRequestDto request, CancellationToken ct)
    {
        var command = new CreateSubscriptionCommand { PlanId = request.PlanId, IsTrial = true };
        return HandleResult(await _mediator.Send(command, ct));
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionCommand command, CancellationToken ct)
        => HandleResult(await _mediator.Send(command, ct));
}
