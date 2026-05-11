using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.Family.Commands;
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

    [HttpPost("posts")]
    [Authorize]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreatePostCommand
        {
            Content = request.Content,
            PostType = request.PostType,
        }, ct);
        return HandleResult(result);
    }

    [HttpPost("posts/{postId:guid}/react")]
    [Authorize]
    public async Task<IActionResult> ToggleReaction(Guid postId, [FromBody] ToggleReactionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ToggleReactionCommand
        {
            PostId = postId,
            ReactionType = request.ReactionType,
        }, ct);
        return HandleResult(result);
    }

    [HttpPost("posts/{postId:guid}/comments")]
    [Authorize]
    public async Task<IActionResult> AddComment(Guid postId, [FromBody] AddCommentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddCommentCommand
        {
            PostId = postId,
            Content = request.Content,
        }, ct);
        return HandleResult(result);
    }

    [HttpDelete("posts/{postId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(Guid postId, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeletePostCommand { PostId = postId }, ct);
        return HandleResult(result);
    }

    [HttpDelete("posts/{postId:guid}/comments/{commentId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(Guid postId, Guid commentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteCommentCommand { PostId = postId, CommentId = commentId }, ct);
        return HandleResult(result);
    }

    [HttpGet("leaderboard")]
    [Authorize]
    public async Task<IActionResult> GetLeaderboard([FromQuery] string category = "weight_loss", CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetGroupLeaderboardQuery { Category = category }, ct);
        return HandleResult(result);
    }
}

public class CreatePostRequest
{
    public string Content { get; set; } = "";
    public string PostType { get; set; } = "UserText";
}

public class ToggleReactionRequest
{
    public string ReactionType { get; set; } = "Like";
}

public class AddCommentRequest
{
    public string Content { get; set; } = "";
}
