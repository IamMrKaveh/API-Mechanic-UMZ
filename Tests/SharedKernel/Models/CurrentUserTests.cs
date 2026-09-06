using SharedKernel.Models;

namespace Tests.SharedKernel.Models;

public class CurrentUserTests
{
    [Fact]
    public void Defaults_AreEmpty()
    {
        var user = new CurrentUser();

        user.UserId.ShouldBe(Guid.Empty);
        user.IsAdmin.ShouldBeFalse();
        user.PhoneNumber.ShouldBeNull();
        user.IpAddress.ShouldBe(string.Empty);
        user.Email.ShouldBeNull();
        user.Username.ShouldBeNull();
        user.GuestToken.ShouldBeNull();
    }

    [Fact]
    public void InitProperties_PreserveValues()
    {
        var id = Guid.NewGuid();
        var user = new CurrentUser
        {
            UserId = id,
            IsAdmin = true,
            PhoneNumber = "09123456789",
            IpAddress = "1.2.3.4",
            Email = "user@example.com",
            Username = "user1",
            GuestToken = "guest-token"
        };

        user.UserId.ShouldBe(id);
        user.IsAdmin.ShouldBeTrue();
        user.PhoneNumber.ShouldBe("09123456789");
        user.IpAddress.ShouldBe("1.2.3.4");
        user.Email.ShouldBe("user@example.com");
        user.Username.ShouldBe("user1");
        user.GuestToken.ShouldBe("guest-token");
    }
}
