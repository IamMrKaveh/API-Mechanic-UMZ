namespace Application.User.Features.Commands.CreateUserAddress;

public record CreateUserAddressCommand(int UserId, CreateUserAddressDto Dto) : IRequest<ServiceResult<int>>;