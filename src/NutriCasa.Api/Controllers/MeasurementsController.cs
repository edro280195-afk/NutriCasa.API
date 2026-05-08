using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.Measurements.Commands.CreateMeasurement;
using NutriCasa.Application.Features.Measurements.DTOs;

namespace NutriCasa.Api.Controllers;

[Authorize]
public class MeasurementsController : BaseApiController
{
    private readonly IMediator _mediator;

    public MeasurementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMeasurementRequest request, CancellationToken ct)
    {
        var command = new CreateMeasurementCommand
        {
            WeightKg = request.WeightKg,
            BodyFatPercentage = request.BodyFatPercentage,
            WaistCm = request.WaistCm,
            HipCm = request.HipCm,
            Notes = request.Notes,
            MeasuredAt = request.MeasuredAt
        };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }
}
