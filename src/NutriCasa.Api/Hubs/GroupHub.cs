using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Features.Family.Commands;

namespace NutriCasa.Api.Hubs;

[Authorize]
public class GroupHub : Hub
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _dbContext;

    public GroupHub(IMediator mediator, IApplicationDbContext dbContext)
    {
        _mediator = mediator;
        _dbContext = dbContext;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdStr = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var membership = await _dbContext.GroupMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null);

            if (membership != null)
            {
                var groupIdStr = membership.GroupId.ToString();
                await Groups.AddToGroupAsync(Context.ConnectionId, groupIdStr);
                Context.Items["GroupId"] = groupIdStr;
            }
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
    }

    public async Task LeaveGroup(string groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
    }

    public async Task<PostResultDto?> CreatePost(string content, string postType)
    {
        var result = await _mediator.Send(new CreatePostCommand
        {
            Content = content,
            PostType = postType,
        });

        if (!result.IsSuccess || result.Value is null)
            return null;

        if (Context.Items.TryGetValue("GroupId", out var groupObj) && groupObj is string groupId)
        {
            await Clients.Group(groupId).SendAsync("PostCreated", result.Value);
        }

        return result.Value;
    }

    public async Task<ReactionResultDto?> ToggleReaction(Guid postId, string reactionType)
    {
        var result = await _mediator.Send(new ToggleReactionCommand
        {
            PostId = postId,
            ReactionType = reactionType,
        });

        if (!result.IsSuccess || result.Value is null)
            return null;

        if (Context.Items.TryGetValue("GroupId", out var groupObj) && groupObj is string groupId)
        {
            await Clients.Group(groupId).SendAsync("ReactionToggled", new
            {
                PostId = postId,
                Reaction = result.Value,
            });
        }

        return result.Value;
    }

    public async Task<CommentResultDto?> AddComment(Guid postId, string content)
    {
        var result = await _mediator.Send(new AddCommentCommand
        {
            PostId = postId,
            Content = content,
        });

        if (!result.IsSuccess || result.Value is null)
            return null;

        if (Context.Items.TryGetValue("GroupId", out var groupObj) && groupObj is string groupId)
        {
            await Clients.Group(groupId).SendAsync("CommentAdded", new
            {
                PostId = postId,
                Comment = result.Value,
            });
        }

        return result.Value;
    }
}
