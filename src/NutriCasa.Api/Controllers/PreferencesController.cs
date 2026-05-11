using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.UserPreferences.Commands;
using NutriCasa.Application.Features.UserPreferences.DTOs;
using NutriCasa.Application.Features.UserPreferences.Queries;

namespace NutriCasa.Api.Controllers;

[Authorize]
public class PreferencesController : BaseApiController
{
    private readonly IMediator _mediator;

    public PreferencesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPreferencesQuery(), ct);
        return HandleResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdatePreferencesRequest request, CancellationToken ct)
    {
        var command = new UpdatePreferencesCommand
        {
            ShareWeight = request.ShareWeight,
            ShareBodyFat = request.ShareBodyFat,
            ShareMeasurements = request.ShareMeasurements,
            SharePhotos = request.SharePhotos,
            ShareCheckIns = request.ShareCheckIns,
            AllowAiMentions = request.AllowAiMentions,
            AllowPush = request.AllowPush,
            AllowEmail = request.AllowEmail,
            WeeklyDigest = request.WeeklyDigest,
            QuietHoursStart = request.QuietHoursStart,
            QuietHoursEnd = request.QuietHoursEnd,
            Timezone = request.Timezone,
            PreferredLanguage = request.PreferredLanguage,
        };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }
}
