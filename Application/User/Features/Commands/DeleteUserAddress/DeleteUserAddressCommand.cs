namespace Application.User.Features.Commands.DeleteUserAddress;

public record DeleteUserAddressCommand(Guid UserId, Guid AddressId) : IRequest<ServiceResult>;