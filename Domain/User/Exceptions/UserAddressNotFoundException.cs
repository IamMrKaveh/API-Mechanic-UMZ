using Domain.Common.Exceptions;
using Domain.User.ValueObjects;

namespace Domain.User.Exceptions;

public sealed class UserAddressNotFoundException : DomainException
{
    public UserAddressId AddressId { get; }

    public override string ErrorCode => "USER_ADDRESS_NOT_FOUND";

    public UserAddressNotFoundException(UserAddressId addressId)
        : base($"Address '{addressId}' was not found for the current user.")
    {
        AddressId = addressId;
    }
}