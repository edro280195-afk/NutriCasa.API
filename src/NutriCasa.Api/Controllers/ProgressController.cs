using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.Progress.Commands;
using NutriCasa.Application.Features.Progress.Queries;

namespace NutriCasa.Api.Controllers;

public class ProgressController : BaseApiController
{
    private readonly IMediator _mediator;

    public ProgressController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("summary")]
    [Authorize]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProgressSummaryQuery(), ct);
        return HandleResult(result);
    }

    [HttpGet("weight-history")]
    [Authorize]
    public async Task<IActionResult> GetWeightHistory(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWeightHistoryQuery(), ct);
        return HandleResult(result);
    }

    [HttpGet("checkins")]
    [Authorize]
    public async Task<IActionResult> GetCheckins(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCheckinHeatmapQuery(), ct);
        return HandleResult(result);
    }

    [HttpGet("macros/weekly")]
    [Authorize]
    public async Task<IActionResult> GetWeeklyMacros(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWeeklyMacrosQuery(), ct);
        return HandleResult(result);
    }

    [HttpGet("photos")]
    [Authorize]
    public async Task<IActionResult> GetPhotos(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPhotosQuery(), ct);
        return HandleResult(result);
    }

    [HttpPost("photos")]
    [Authorize]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto([FromForm] UploadPhotoRequest request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest(new { error = "El archivo es requerido." });

        await using var stream = request.File.OpenReadStream();
        var result = await _mediator.Send(new UploadPhotoCommand
        {
            FileStream = stream,
            FileName = request.File.FileName,
            ContentType = request.File.ContentType,
            FileSize = request.File.Length,
            Angle = request.Angle,
            Visibility = request.Visibility ?? "Private",
            TakenAt = request.TakenAt ?? "",
        }, ct);
        return HandleResult(result);
    }

    [HttpDelete("photos/{photoId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeletePhoto(Guid photoId, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeletePhotoCommand { PhotoId = photoId }, ct);
        return HandleResult(result);
    }

    [HttpPatch("photos/{photoId:guid}/visibility")]
    [Authorize]
    public async Task<IActionResult> UpdateVisibility(Guid photoId, [FromBody] UpdateVisibilityRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdatePhotoVisibilityCommand
        {
            PhotoId = photoId,
            Visibility = request.Visibility,
        }, ct);
        return HandleResult(result);
    }
}

public class UploadPhotoRequest
{
    public IFormFile File { get; set; } = null!;
    public string? Angle { get; set; }
    public string? Visibility { get; set; }
    public string? TakenAt { get; set; }
}

public class UpdateVisibilityRequest
{
    public string Visibility { get; set; } = "Private";
}
