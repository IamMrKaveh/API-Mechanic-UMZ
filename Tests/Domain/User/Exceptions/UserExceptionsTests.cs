using Domain.User.Exceptions;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.User.Exceptions;

public class UserExceptionsTests
{
    [Fact]
    public void InvalidPhoneNumberException_ExposesPhoneNumberAndErrorCodeAndMessage()
    {
        var sut = new InvalidPhoneNumberException("07123456");

        sut.PhoneNumber.ShouldBe("07123456");
        sut.ErrorCode.ShouldBe("INVALID_PHONE_NUMBER");
        sut.Message.ShouldContain("07123456");
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void UserAddressNotFoundException_ExposesAddressIdAndErrorCodeAndMessage()
    {
        var addressId = UserAddressId.NewId();

        var sut = new UserAddressNotFoundException(addressId);

        sut.AddressId.ShouldBe(addressId);
        sut.ErrorCode.ShouldBe("USER_ADDRESS_NOT_FOUND");
        sut.Message.ShouldContain(addressId.ToString());
        sut.ShouldBeAssignableTo<DomainException>();
    }
}
