using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NutriCasa.Application.Features.Family.Commands;

namespace NutriCasa.Api.Hubs;

[Authorize]
public class GroupHub : Hub
{
    private readonly IMediator _mediator;

    public GroupHub(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task OnConnectedAsync()
    {
        var groupId = Context.GetHttpContext()?.Request.Query["groupId"].FirstOrDefault();
        if (!string.IsNullOrEmpty(groupId))
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);

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

        await Clients.Group(result.Value.PostId.ToString()).SendAsync("PostCreated", result.Value);

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

        await Clients.Group(postId.ToString()).SendAsync("ReactionToggled", new
        {
            PostId = postId,
            Reaction = result.Value,
        });

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

        await Clients.Group(postId.ToString()).SendAsync("CommentAdded", new
        {
            PostId = postId,
            Comment = result.Value,
        });

        return result.Value;
    }
}
