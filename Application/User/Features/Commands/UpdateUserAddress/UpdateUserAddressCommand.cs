namespace Application.User.Features.Commands.UpdateUserAddress;

public record UpdateUserAddressCommand(int UserId, int AddressId, UpdateUserAddressDto Dto) : IRequest<ServiceResult>;