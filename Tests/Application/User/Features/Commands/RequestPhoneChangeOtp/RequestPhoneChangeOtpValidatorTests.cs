using Application.User.Features.Commands.RequestPhoneChangeOtp;

namespace Tests.Application.User.Features.Commands.RequestPhoneChangeOtp;

public class RequestPhoneChangeOtpValidatorTests
{
    private readonly RequestPhoneChangeOtpValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0912345")]
    [InlineData("08123456789")]
    [InlineData("not-a-phone")]
    public void Validate_WithInvalidPhoneNumber_Fails(string phoneNumber)
    {
        var result = _validator.Validate(new RequestPhoneChangeOtpCommand(phoneNumber));

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("09123456789")]
    [InlineData("09351234567")]
    public void Validate_WithValidPhoneNumber_Passes(string phoneNumber)
    {
        var result = _validator.Validate(new RequestPhoneChangeOtpCommand(phoneNumber));

        result.IsValid.ShouldBeTrue();
    }
}
