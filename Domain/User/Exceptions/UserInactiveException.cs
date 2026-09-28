using Domain.User.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.User.Exceptions;

public sealed class UserInactiveException(UserId userId)
    : ConflictException<UserId>(
        "USER_INACTIVE",
        userId,
        $"User {userId} is inactive.")
{
    public UserId UserId => Value;
}
