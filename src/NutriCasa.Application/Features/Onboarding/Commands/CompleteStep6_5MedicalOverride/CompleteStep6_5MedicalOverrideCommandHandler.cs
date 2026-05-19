using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Services;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep6_5MedicalOverride;

public class CompleteStep6_5MedicalOverrideCommandHandler : IRequestHandler<CompleteStep6_5MedicalOverrideCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;

    public CompleteStep6_5MedicalOverrideCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(CompleteStep6_5MedicalOverrideCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        if (!request.DisclaimerAccepted)
            return Result.Failure("Debes aceptar el disclaimer médico.", "DISCLAIMER_NOT_ACCEPTED");

        var user = await _context.Users
            .Include(u => u.MedicalProfile)
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId, cancellationToken);

        if (user is null)
            return Result.Failure("Usuario no encontrado.", "NOT_FOUND");

        if (user.MedicalProfile is null)
            return Result.Failure("No hay perfil médico registrado. Completa el paso 6 primero.", "NO_MEDICAL_PROFILE");

        user.MedicalProfile.RequiresHumanReview = MedicalSafetyRules.RequiresHumanReview(user.MedicalProfile);

        if (!user.MedicalProfile.RequiresHumanReview)
            return Result.Failure("Tu perfil médico no requiere override.", "OVERRIDE_NOT_REQUIRED");

        int age = DateTime.UtcNow.Year - user.BirthDate.Year;
        if (user.BirthDate > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-age))) age--;

        if (!MedicalSafetyRules.CanAcceptOverride(user.MedicalProfile, age))
            return Result.Failure(
                MedicalSafetyRules.GetAbsoluteBlockMessage(user.MedicalProfile, age),
                "MEDICAL_OVERRIDE_NOT_ALLOWED");

        if (!_passwordHasher.Verify(request.PasswordConfirmation, user.PasswordHash))
            return Result.Failure("La contraseña no es correcta.", "INVALID_PASSWORD");

        var disclaimer = request.DisclaimerVersionId.HasValue
            ? await _context.DisclaimerVersions
                .FirstOrDefaultAsync(d => d.Id == request.DisclaimerVersionId.Value
                                       && d.DisclaimerType == "override"
                                       && d.IsCurrent, cancellationToken)
            : await _context.DisclaimerVersions
                .FirstOrDefaultAsync(d => d.DisclaimerType == "override"
                                       && d.IsCurrent, cancellationToken);

        if (disclaimer is null)
            return Result.Failure("El disclaimer de override no está disponible.", "INVALID_DISCLAIMER");

        user.MedicalProfile.OverrideAcceptedAt = DateTime.UtcNow;
        user.MedicalProfile.OverrideDisclaimerVersionId = disclaimer.Id;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            EntityType = "medical_profiles",
            EntityId = user.MedicalProfile.Id,
            Action = "medical_override_accepted",
            NewValues = JsonSerializer.Serialize(new
            {
                override_accepted_at = DateTime.UtcNow,
                disclaimer_version_id = disclaimer.Id.ToString(),
                ip_address = _currentUserService.IpAddress
            }),
            IpAddress = System.Net.IPAddress.TryParse(_currentUserService.IpAddress, out var ip) ? ip : null,
            UserAgent = _currentUserService.UserAgent,
            CreatedAt = DateTime.UtcNow,
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
