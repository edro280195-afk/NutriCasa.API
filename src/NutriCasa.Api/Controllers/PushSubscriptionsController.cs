using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Features.PushSubscriptions.Commands;
using NutriCasa.Application.Features.PushSubscriptions.DTOs;
using NutriCasa.Application.Features.PushSubscriptions.Queries;

namespace NutriCasa.Api.Controllers;

[Authorize]
public class PushSubscriptionsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPushNotificationService _pushService;

    public PushSubscriptionsController(IMediator mediator, IPushNotificationService pushService)
    {
        _mediator = mediator;
        _pushService = pushService;
    }

    [HttpGet("vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        return Ok(new { publicKey = _pushService.GetVapidPublicKey() });
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubscribeCommand
        {
            Endpoint = request.Endpoint,
            P256dhKey = request.P256dhKey,
            AuthKey = request.AuthKey,
        }, ct);
        return HandleResult(result);
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequestDto request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UnsubscribeCommand { Endpoint = request.Endpoint }, ct);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetSubscriptions(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSubscriptionsQuery(), ct);
        return HandleResult(result);
    }
}
