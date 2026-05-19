using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.Challenges.Commands.CreateChallenge;
using NutriCasa.Application.Features.Challenges.Commands.JoinChallenge;
using NutriCasa.Application.Features.Challenges.Commands.LeaveChallenge;
using NutriCasa.Application.Features.Challenges.Queries.GetActiveChallenges;
using NutriCasa.Application.Features.Challenges.Queries.GetChallengeLeaderboard;
using NutriCasa.Application.Features.Challenges.Queries.GetMyChallenges;

namespace NutriCasa.Api.Controllers;

[Authorize]
public class ChallengeController : BaseApiController
{
    private readonly IMediator _mediator;

    public ChallengeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        return HandleResult(await _mediator.Send(new GetActiveChallengesQuery()));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        return HandleResult(await _mediator.Send(new GetMyChallengesQuery()));
    }

    [HttpGet("{id:guid}/leaderboard")]
    public async Task<IActionResult> GetLeaderboard(Guid id)
    {
        return HandleResult(await _mediator.Send(new GetChallengeLeaderboardQuery { ChallengeId = id }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChallengeCommand command)
    {
        return HandleResult(await _mediator.Send(command));
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(Guid id)
    {
        return HandleResult(await _mediator.Send(new JoinChallengeCommand { ChallengeId = id }));
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id)
    {
        return HandleResult(await _mediator.Send(new LeaveChallengeCommand { ChallengeId = id }));
    }
}
