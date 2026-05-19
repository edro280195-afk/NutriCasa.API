using MediatR;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.AccountDeletion.Commands.CancelDeletion;

public record CancelAccountDeletionCommand : IRequest<Result>;
