using Domain.User.ValueObjects;

namespace Infrastructure.User.Converters;

internal sealed class UserAddressIdConverter : StronglyTypedIdConverter<UserAddressId>
{
    public UserAddressIdConverter() : base(UserAddressId.From)
    {
    }
}