using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.Admin.Commands.DeletePost;
using NutriCasa.Application.Features.Admin.Commands.UpdateUserRole;
using NutriCasa.Application.Features.Admin.DTOs;
using NutriCasa.Application.Features.Admin.Queries.GetDashboard;
using NutriCasa.Application.Features.Admin.Queries.GetPosts;
using NutriCasa.Application.Features.Admin.Queries.GetUsers;
using NutriCasa.Infrastructure.Persistence;
using NutriCasa.Infrastructure.Persistence.Seeds;

namespace NutriCasa.Api.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
        => HandleResult(await _mediator.Send(new GetDashboardQuery(), ct));

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? role, CancellationToken ct)
        => HandleResult(await _mediator.Send(new GetUsersQuery { Search = search, Role = role }, ct));

    [HttpPatch("users/{userId}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleRequestDto request, CancellationToken ct)
        => HandleResult(await _mediator.Send(new UpdateUserRoleCommand { UserId = userId, Role = request.Role }, ct));

    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts(CancellationToken ct)
        => HandleResult(await _mediator.Send(new GetPostsQuery(), ct));

    [HttpDelete("posts/{postId}")]
    public async Task<IActionResult> DeletePost(Guid postId, CancellationToken ct)
        => HandleResult(await _mediator.Send(new DeletePostCommand { PostId = postId }, ct));

    [HttpPost("seed-curated-recipes")]
    public async Task<IActionResult> SeedCuratedRecipes(
        [FromServices] ApplicationDbContext context,
        CancellationToken ct)
    {
        await CuratedRecipeSeeder.SeedAsync(context);
        return Ok(new { message = "Recetas curadas sembradas exitosamente" });
    }
}
