using Domain.User.ValueObjects;

namespace Domain.User.Exceptions;

public sealed class UserAddressNotFoundException(UserAddressId addressId) : DomainException($"Address '{addressId}' was not found for the current user.")
{
    public UserAddressId AddressId { get; } = addressId;

    public override string ErrorCode => "USER_ADDRESS_NOT_FOUND";
}