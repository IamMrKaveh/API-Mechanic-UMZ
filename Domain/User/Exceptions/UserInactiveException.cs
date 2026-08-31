using Domain.User.ValueObjects;

namespace Domain.User.Exceptions;

public sealed class UserInactiveException(UserId userId) : DomainException($"User {userId} is inactive.")
{
    public UserId UserId { get; } = userId;
    public override string ErrorCode => "USER_INACTIVE";
}
