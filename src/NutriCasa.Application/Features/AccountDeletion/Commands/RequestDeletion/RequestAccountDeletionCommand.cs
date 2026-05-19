using MediatR;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.AccountDeletion.Commands.RequestDeletion;

public record RequestAccountDeletionCommand : IRequest<Result<DeletionScheduledResponse>>;

public record DeletionScheduledResponse
{
    public DateTime DeletionRequestedAt { get; init; }
    public DateTime DeletionScheduledFor { get; init; }
    public int GraceDays { get; init; }
}
