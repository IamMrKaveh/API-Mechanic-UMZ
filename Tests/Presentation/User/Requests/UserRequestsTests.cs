using Presentation.User.Requests;

namespace Tests.Presentation.User.Requests;

public class UserRequestsTests
{
    [Fact]
    public void CreateUserAddressRequest_WithRequiredValues_SetsCorrectly()
    {
        var request = new CreateUserAddressRequest("Home", "Ali", "09123456789", "Tehran", "Tehran", "Street 1", "12345");

        request.Title.ShouldBe("Home");
        request.ReceiverName.ShouldBe("Ali");
        request.PhoneNumber.ShouldBe("09123456789");
        request.Province.ShouldBe("Tehran");
        request.City.ShouldBe("Tehran");
        request.Address.ShouldBe("Street 1");
        request.PostalCode.ShouldBe("12345");
        request.IsDefault.ShouldBeFalse();
        request.Latitude.ShouldBeNull();
        request.Longitude.ShouldBeNull();
    }

    [Fact]
    public void CreateUserAddressRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new CreateUserAddressRequest("Home", "Ali", "09123456789", "Tehran", "Tehran", "Street 1", "12345", true, 35.7m, 51.4m);

        request.IsDefault.ShouldBeTrue();
        request.Latitude.ShouldBe(35.7m);
        request.Longitude.ShouldBe(51.4m);
    }

    [Fact]
    public void UpdateUserAddressRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new UpdateUserAddressRequest("Work", "Sara", "09987654321", "Tehran", "Tehran", "Street 2", "54321", true, null, null);

        request.Title.ShouldBe("Work");
        request.IsDefault.ShouldBeTrue();
        request.Latitude.ShouldBeNull();
    }

    [Fact]
    public void UpdateProfileRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new UpdateProfileRequest("Ali", "Rezaei");

        request.FirstName.ShouldBe("Ali");
        request.LastName.ShouldBe("Rezaei");
    }

    [Fact]
    public void ChangePasswordRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new ChangePasswordRequest("old", "new", "new");

        request.CurrentPassword.ShouldBe("old");
        request.NewPassword.ShouldBe("new");
        request.ConfirmPassword.ShouldBe("new");
    }

    [Fact]
    public void ChangePhoneNumberRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new ChangePhoneNumberRequest("09987654321", "123456");

        request.NewPhoneNumber.ShouldBe("09987654321");
        request.OtpCode.ShouldBe("123456");
    }

    [Fact]
    public void ChangeUserRoleRequest_WithIsAdmin_SetsCorrectly()
    {
        new ChangeUserRoleRequest(true).IsAdmin.ShouldBeTrue();
        new ChangeUserRoleRequest(false).IsAdmin.ShouldBeFalse();
    }

    [Fact]
    public void ChangeUserStatusRequest_WithIsActive_SetsCorrectly()
    {
        new ChangeUserStatusRequest(true).IsActive.ShouldBeTrue();
        new ChangeUserStatusRequest(false).IsActive.ShouldBeFalse();
    }

    [Fact]
    public void AdminCreateUserRequest_WithRequiredValues_SetsCorrectly()
    {
        var request = new AdminCreateUserRequest("09123456789", null, null, null);

        request.PhoneNumber.ShouldBe("09123456789");
        request.FirstName.ShouldBeNull();
        request.LastName.ShouldBeNull();
        request.Email.ShouldBeNull();
        request.IsAdmin.ShouldBeFalse();
    }

    [Fact]
    public void AdminCreateUserRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new AdminCreateUserRequest("09123456789", "Ali", "Rezaei", "a@x.com", true);

        request.FirstName.ShouldBe("Ali");
        request.LastName.ShouldBe("Rezaei");
        request.Email.ShouldBe("a@x.com");
        request.IsAdmin.ShouldBeTrue();
    }

    [Fact]
    public void UpdateProfileRequest_IsRecord_EqualityWorks()
    {
        var request1 = new UpdateProfileRequest("Ali", "Rezaei");
        var request2 = new UpdateProfileRequest("Ali", "Rezaei");
        var request3 = new UpdateProfileRequest("Sara", "Rezaei");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
