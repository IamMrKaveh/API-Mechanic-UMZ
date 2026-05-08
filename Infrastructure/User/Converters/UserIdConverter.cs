using Domain.User.ValueObjects;

namespace Infrastructure.User.Converters;

internal sealed class UserIdConverter : StronglyTypedIdConverter<UserId>
{
    public UserIdConverter() : base(UserId.From)
    {
    }
}