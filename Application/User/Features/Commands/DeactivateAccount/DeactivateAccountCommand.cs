using Application.Common.Results;

namespace Application.User.Features.Commands.DeactivateAccount;

public record DeactivateAccountCommand(Guid UserId) : IRequest<ServiceResult>;