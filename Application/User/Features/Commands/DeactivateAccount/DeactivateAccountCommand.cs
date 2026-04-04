using Application.Common.Results;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.DeactivateAccount;

public record DeactivateAccountCommand(UserId UserId) : IRequest<ServiceResult>;