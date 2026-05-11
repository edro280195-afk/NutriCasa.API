using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.Notifications.Commands;
using NutriCasa.Application.Features.Notifications.Queries;

namespace NutriCasa.Api.Controllers;

[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetNotificationsQuery { Skip = skip, Take = take }, ct);
        return HandleResult(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUnreadCountQuery(), ct);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new MarkNotificationReadCommand { NotificationId = id }, ct);
        return HandleResult(result);
    }
}
