using Domain.User.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.User.Exceptions;

public sealed class UserAddressNotFoundException(UserAddressId addressId)
    : NotFoundException<UserAddressId>(
        "USER_ADDRESS_NOT_FOUND",
        addressId,
        $"Address '{addressId}' was not found for the current user.")
{
    public UserAddressId AddressId => Value;
}
