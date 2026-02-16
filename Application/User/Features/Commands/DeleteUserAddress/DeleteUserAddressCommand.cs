namespace Application.User.Features.Commands.DeleteUserAddress;

public record DeleteUserAddressCommand(int UserId, int AddressId) : IRequest<ServiceResult>;