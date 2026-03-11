using Domain.User.ValueObjects;

namespace Domain.User.Exceptions;

public sealed class UserAddressNotFoundException(UserAddressId addressId) : Exception($"Address '{addressId}' was not found for the current user.")
{
}