using Application.User.Features.Shared;

namespace Tests.Application.User.Features.Shared;

public class UserDtosTests
{
    [Fact]
    public void UserProfileDto_Defaults_AreEmpty()
    {
        var dto = new UserProfileDto();

        dto.Id.ShouldBe(default(Guid));
        dto.PhoneNumber.ShouldBe(string.Empty);
        dto.FirstName.ShouldBeNull();
        dto.UserAddresses.ShouldNotBeNull();
        dto.UserAddresses.ShouldBeEmpty();
    }

    [Fact]
    public void UserAddressDto_RoundTrip()
    {
        var dto = new UserAddressDto
        {
            Id = Guid.NewGuid(), Title = "Home", ReceiverName = "Ali",
            PhoneNumber = "09120000000", Province = "Tehran", City = "Tehran",
            Address = "St 1", PostalCode = "1234567890",
            Latitude = 35.7m, Longitude = 51.4m, IsDefault = true
        };

        dto.IsDefault.ShouldBeTrue();
        dto.Latitude.ShouldBe(35.7m);
    }

    [Fact]
    public void UserDashboardDto_Defaults_AreEmpty()
    {
        var dto = new UserDashboardDto();

        dto.UserProfile.ShouldBeNull();
        dto.RecentOrders.ShouldBeEmpty();
        dto.TotalSpent.ShouldBe(0m);
    }
}
