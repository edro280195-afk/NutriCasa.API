using MediatR;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Measurements.DTOs;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.Measurements.Commands.CreateMeasurement;

public class CreateMeasurementCommandHandler : IRequestHandler<CreateMeasurementCommand, Result<MeasurementResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateMeasurementCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<MeasurementResponse>> Handle(CreateMeasurementCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<MeasurementResponse>.Failure("No autenticado.", "UNAUTHORIZED");

        var measurement = new BodyMeasurement
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId.Value,
            WeightKg = request.WeightKg,
            BodyFatPercentage = request.BodyFatPercentage,
            WaistCm = request.WaistCm,
            HipCm = request.HipCm,
            Notes = request.Notes,
            MeasuredAt = request.MeasuredAt ?? DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow
        };

        _context.BodyMeasurements.Add(measurement);
        await _context.SaveChangesAsync(ct);

        return Result<MeasurementResponse>.Success(new MeasurementResponse
        {
            Id = measurement.Id,
            WeightKg = measurement.WeightKg,
            BodyFatPercentage = measurement.BodyFatPercentage,
            WaistCm = measurement.WaistCm,
            HipCm = measurement.HipCm,
            Notes = measurement.Notes,
            MeasuredAt = measurement.MeasuredAt,
            CreatedAt = measurement.CreatedAt
        });
    }
}
