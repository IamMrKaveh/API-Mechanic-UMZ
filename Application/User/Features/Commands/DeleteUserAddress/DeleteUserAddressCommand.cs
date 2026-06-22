namespace Application.User.Features.Commands.DeleteUserAddress;

public record DeleteUserAddressCommand(
    Guid AddressId)
    : ICommand;