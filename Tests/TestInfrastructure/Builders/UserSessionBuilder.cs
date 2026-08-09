using Bogus;
using Domain.Security.Aggregates;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public class UserSessionBuilder
{
    private static readonly Faker Faker = new();

    private SessionId _id = SessionId.NewId();
    private UserId _userId = UserId.NewId();
    private RefreshToken _refreshToken = RefreshToken.Generate();
    private DeviceInfo _deviceInfo = DeviceInfo.Create(Faker.Internet.UserAgent());
    private IpAddress _ipAddress = IpAddress.Create(Faker.Internet.Ip());
    private DateTime _expiresAt = DateTime.UtcNow.AddDays(7);

    public UserSessionBuilder WithId(SessionId id)
    {
        _id = id;
        return this;
    }

    public UserSessionBuilder WithUserId(UserId userId)
    {
        _userId = userId;
        return this;
    }

    public UserSessionBuilder WithRefreshToken(RefreshToken refreshToken)
    {
        _refreshToken = refreshToken;
        return this;
    }

    public UserSessionBuilder WithDeviceInfo(DeviceInfo deviceInfo)
    {
        _deviceInfo = deviceInfo;
        return this;
    }

    public UserSessionBuilder WithDeviceInfo(string value)
    {
        _deviceInfo = DeviceInfo.Create(value);
        return this;
    }

    public UserSessionBuilder WithIpAddress(IpAddress ipAddress)
    {
        _ipAddress = ipAddress;
        return this;
    }

    public UserSessionBuilder WithIpAddress(string value)
    {
        _ipAddress = IpAddress.Create(value);
        return this;
    }

    public UserSessionBuilder WithExpiresAt(DateTime expiresAt)
    {
        _expiresAt = expiresAt;
        return this;
    }

    public UserSession Build() =>
        UserSession.Create(_id, _userId, _refreshToken, _deviceInfo, _ipAddress, _expiresAt);
}
