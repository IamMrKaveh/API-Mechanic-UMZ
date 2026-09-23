using Application.Auth.Features.Shared;
using Application.User.Features.Shared;
using Application.User.Mapping;
using Domain.User.ValueObjects;
using Mapster;

namespace Tests.Application.User.Mapping;

public class UserMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public UserMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new UserMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_User_ToUserProfileDto_MapsIdentityContactsAndFlags()
    {
        var user = new UserBuilder()
            .WithFullName(FullName.Create("Ali", "Rezaei"))
            .WithEmail("ali@example.com")
            .WithPhoneNumber(PhoneNumber.Create("09120000000"))
            .Build();

        var dto = _mapper.Map<UserProfileDto>(user);

        dto.Id.ShouldBe(user.Id.Value);
        dto.PhoneNumber.ShouldBe("09120000000");
        dto.FirstName.ShouldBe("Ali");
        dto.LastName.ShouldBe("Rezaei");
        dto.Email.ShouldBe("ali@example.com");
        dto.IsActive.ShouldBe(user.IsActive);
        dto.IsAdmin.ShouldBe(user.IsAdmin);
        dto.CreatedAt.ShouldBe(user.CreatedAt);
        dto.LastLoginAt.ShouldBe(user.LastLoginAt);
        dto.UserAddresses.ShouldBeEmpty();
    }

    [Fact]
    public void Map_User_WithoutPhone_MapsEmptyPhoneNumber()
    {
        var user = new UserBuilder().WithPhoneNumber(null).Build();

        var dto = _mapper.Map<UserProfileDto>(user);

        dto.PhoneNumber.ShouldBe(string.Empty);
    }

    [Fact]
    public void Map_UserSession_ToUserSessionDto_MapsNetworkAndLifetime()
    {
        var session = new UserSessionBuilder()
            .WithIpAddress("10.0.0.5")
            .WithDeviceInfo("TestAgent/1.0")
            .Build();

        var dto = _mapper.Map<UserSessionDto>(session);

        dto.Id.ShouldBe(session.Id.Value);
        dto.CreatedByIp.ShouldBe("10.0.0.5");
        dto.DeviceInfo.ShouldBe("TestAgent/1.0");
        dto.CreatedAt.ShouldBe(session.CreatedAt);
        dto.ExpiresAt.ShouldBe(session.ExpiresAt);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new UserMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
