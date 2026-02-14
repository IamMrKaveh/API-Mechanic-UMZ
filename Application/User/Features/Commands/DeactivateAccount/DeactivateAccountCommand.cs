namespace Application.User.Features.Commands.DeactivateAccount;

public record DeactivateAccountCommand(int UserId) : IRequest<ServiceResult>;