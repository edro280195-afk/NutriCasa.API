using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.AccountDeletion.Commands.CancelDeletion;
using NutriCasa.Application.Features.AccountDeletion.Commands.RequestDeletion;
using NutriCasa.Application.Features.AccountDeletion.Queries;

namespace NutriCasa.Api.Controllers;

public class AccountDeletionController : BaseApiController
{
    private readonly IMediator _mediator;

    public AccountDeletionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("request")]
    [Authorize]
    public async Task<IActionResult> RequestDeletion(CancellationToken ct)
    {
        var result = await _mediator.Send(new RequestAccountDeletionCommand(), ct);
        return HandleResult(result);
    }

    [HttpPost("cancel")]
    [Authorize]
    public async Task<IActionResult> CancelDeletion(CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelAccountDeletionCommand(), ct);
        return HandleResult(result);
    }

    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDeletionStatusQuery(), ct);
        return HandleResult(result);
    }
}
