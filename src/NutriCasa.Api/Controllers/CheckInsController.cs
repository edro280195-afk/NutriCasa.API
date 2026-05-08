using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.CheckIns.Commands.CreateCheckIn;
using NutriCasa.Application.Features.CheckIns.DTOs;

namespace NutriCasa.Api.Controllers;

[Authorize]
public class CheckInsController : BaseApiController
{
    private readonly IMediator _mediator;

    public CheckInsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCheckInRequest request, CancellationToken ct)
    {
        var command = new CreateCheckInCommand
        {
            HungerLevel = request.HungerLevel,
            EnergyLevel = request.EnergyLevel,
            MoodLevel = request.MoodLevel,
            DifficultyLevel = request.DifficultyLevel,
            SleepHours = request.SleepHours,
            WaterLiters = request.WaterLiters,
            HadCheatMeal = request.HadCheatMeal,
            CheatDescription = request.CheatDescription,
            Notes = request.Notes,
            CheckInDate = request.CheckInDate
        };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }
}
