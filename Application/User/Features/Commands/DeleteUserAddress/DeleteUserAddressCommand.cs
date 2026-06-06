namespace Application.User.Features.Commands.DeleteUserAddress;

public record DeleteUserAddressCommand(Guid AddressId) : IRequest<ServiceResult>;