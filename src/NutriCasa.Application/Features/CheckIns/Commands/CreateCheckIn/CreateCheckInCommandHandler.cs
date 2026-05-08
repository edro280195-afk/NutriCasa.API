using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.CheckIns.DTOs;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.CheckIns.Commands.CreateCheckIn;

public class CreateCheckInCommandHandler : IRequestHandler<CreateCheckInCommand, Result<CheckInResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateCheckInCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CheckInResponse>> Handle(CreateCheckInCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<CheckInResponse>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;
        var checkInDate = request.CheckInDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var existing = await _context.DailyCheckIns
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CheckInDate == checkInDate, ct);

        if (existing is not null)
            return Result<CheckInResponse>.Failure("Ya realizaste el check-in hoy.", "CONFLICT");

        var checkIn = new DailyCheckIn
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CheckInDate = checkInDate,
            HungerLevel = request.HungerLevel,
            EnergyLevel = request.EnergyLevel,
            MoodLevel = request.MoodLevel,
            DifficultyLevel = request.DifficultyLevel,
            SleepHours = request.SleepHours,
            WaterLiters = request.WaterLiters,
            HadCheatMeal = request.HadCheatMeal,
            CheatDescription = request.CheatDescription,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.DailyCheckIns.Add(checkIn);
        await _context.SaveChangesAsync(ct);

        return Result<CheckInResponse>.Success(new CheckInResponse
        {
            Id = checkIn.Id,
            CheckInDate = checkIn.CheckInDate,
            HungerLevel = checkIn.HungerLevel,
            EnergyLevel = checkIn.EnergyLevel,
            MoodLevel = checkIn.MoodLevel,
            WaterLiters = checkIn.WaterLiters,
            HadCheatMeal = checkIn.HadCheatMeal,
            Notes = checkIn.Notes,
            CreatedAt = checkIn.CreatedAt
        });
    }
}
