using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Api.Hubs;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Features.Family.Commands;
using NutriCasa.Application.Features.Family.Commands.ChangeMemberRole;
using NutriCasa.Application.Features.Family.Commands.CreateSubgroup;
using NutriCasa.Application.Features.Family.Commands.RegenerateInviteCode;
using NutriCasa.Application.Features.Family.Commands.RemoveMember;
using NutriCasa.Application.Features.Family.Commands.TransferOwnership;
using NutriCasa.Application.Features.Family.Queries;
using NutriCasa.Application.Features.Family.Queries.GetInviteCode;

namespace NutriCasa.Api.Controllers;

public class FamilyController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IHubContext<GroupHub> _hubContext;
    private readonly IApplicationDbContext _context;

    public FamilyController(
        IMediator mediator,
        IHubContext<GroupHub> hubContext,
        IApplicationDbContext context)
    {
        _mediator = mediator;
        _hubContext = hubContext;
        _context = context;
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

    [HttpPost("posts/upload-image")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPostImage(IFormFile file, CancellationToken ct)
    {
        var result = await _mediator.Send(new UploadPostImageCommand
        {
            FileStream = file.OpenReadStream(),
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
        }, ct);
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
            ImageUrl = request.ImageUrl,
        }, ct);

        if (result.IsSuccess && result.Value != null)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userId, out var userGuid))
            {
                var membership = await _context.GroupMemberships
                    .FirstOrDefaultAsync(m => m.UserId == userGuid && m.LeftAt == null, ct);
                if (membership != null)
                {
                    await _hubContext.Clients.Group(membership.GroupId.ToString())
                        .SendAsync("PostCreated", result.Value, cancellationToken: ct);
                }
            }
        }

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

        if (result.IsSuccess && result.Value != null)
        {
            var post = await _context.GroupPosts.FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post != null)
            {
                await _hubContext.Clients.Group(post.GroupId.ToString())
                    .SendAsync("ReactionToggled", new { PostId = postId, Reaction = result.Value }, cancellationToken: ct);
            }
        }

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

        if (result.IsSuccess && result.Value != null)
        {
            var post = await _context.GroupPosts.FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post != null)
            {
                await _hubContext.Clients.Group(post.GroupId.ToString())
                    .SendAsync("CommentAdded", new { PostId = postId, Comment = result.Value }, cancellationToken: ct);
            }
        }

        return HandleResult(result);
    }

    [HttpDelete("posts/{postId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(Guid postId, CancellationToken ct)
    {
        var post = await _context.GroupPosts.FirstOrDefaultAsync(p => p.Id == postId, ct);

        var result = await _mediator.Send(new DeletePostCommand { PostId = postId }, ct);

        if (result.IsSuccess && post != null)
        {
            await _hubContext.Clients.Group(post.GroupId.ToString())
                .SendAsync("PostDeleted", postId, cancellationToken: ct);
        }

        return HandleResult(result);
    }

    [HttpDelete("posts/{postId:guid}/comments/{commentId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(Guid postId, Guid commentId, CancellationToken ct)
    {
        var post = await _context.GroupPosts.FirstOrDefaultAsync(p => p.Id == postId, ct);

        var result = await _mediator.Send(new DeleteCommentCommand { PostId = postId, CommentId = commentId }, ct);

        if (result.IsSuccess && post != null)
        {
            await _hubContext.Clients.Group(post.GroupId.ToString())
                .SendAsync("CommentDeleted", new { PostId = postId, CommentId = commentId }, cancellationToken: ct);
        }

        return HandleResult(result);
    }

    [HttpGet("leaderboard")]
    [Authorize]
    public async Task<IActionResult> GetLeaderboard([FromQuery] string category = "weight_loss", CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetGroupLeaderboardQuery { Category = category }, ct);
        return HandleResult(result);
    }

    // ── Group Management ──

    [HttpPost("subgroups")]
    [Authorize]
    public async Task<IActionResult> CreateSubgroup([FromBody] CreateSubgroupRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateSubgroupCommand
        {
            Name = request.Name,
            Description = request.Description,
        }, ct);
        return HandleResult(result);
    }

    [HttpPost("transfer-ownership")]
    [Authorize]
    public async Task<IActionResult> TransferOwnership([FromBody] TransferOwnershipRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new TransferOwnershipCommand
        {
            TargetUserId = request.TargetUserId,
        }, ct);
        return HandleResult(result);
    }

    [HttpPatch("members/{userId:guid}/role")]
    [Authorize]
    public async Task<IActionResult> ChangeMemberRole(Guid userId, [FromBody] ChangeMemberRoleRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ChangeMemberRoleCommand
        {
            TargetUserId = userId,
            NewRole = request.Role,
        }, ct);
        return HandleResult(result);
    }

    [HttpDelete("members/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveMember(Guid userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new RemoveMemberCommand
        {
            TargetUserId = userId,
        }, ct);
        return HandleResult(result);
    }

    [HttpGet("invite-code")]
    [Authorize]
    public async Task<IActionResult> GetInviteCode(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetInviteCodeQuery(), ct);
        return HandleResult(result);
    }

    [HttpPost("invite-code/regenerate")]
    [Authorize]
    public async Task<IActionResult> RegenerateInviteCode(CancellationToken ct)
    {
        var result = await _mediator.Send(new RegenerateInviteCodeCommand(), ct);
        return HandleResult(result);
    }
}

public class CreateSubgroupRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public class TransferOwnershipRequest
{
    public Guid TargetUserId { get; set; }
}

public class ChangeMemberRoleRequest
{
    public string Role { get; set; } = "member";
}

public class CreatePostRequest
{
    public string Content { get; set; } = "";
    public string PostType { get; set; } = "UserText";
    public string? ImageUrl { get; set; }
}

public class ToggleReactionRequest
{
    public string ReactionType { get; set; } = "Like";
}

public class AddCommentRequest
{
    public string Content { get; set; } = "";
}
