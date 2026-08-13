using Application.Auth.Features.Commands.SendOtp;

namespace Tests.Application.Auth.Features.Commands.SendOtp;

public class SendOtpValidatorTests
{
    private readonly SendOtpValidator _sut = new();

    [Theory]
    [InlineData("09123456789")]
    [InlineData("09000000000")]
    [InlineData("09999999999")]
    public void Validate_WithValidIranianMobile_IsValid(string phoneNumber)
    {
        var command = new SendOtpCommand(phoneNumber);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyPhoneNumber_FailsOnPhoneNumber(string phoneNumber)
    {
        var command = new SendOtpCommand(phoneNumber);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SendOtpCommand.PhoneNumber));
    }

    [Theory]
    [InlineData("0912345678")]
    [InlineData("091234567890")]
    [InlineData("08123456789")]
    [InlineData("9123456789")]
    [InlineData("abc12345678")]
    public void Validate_WithInvalidPhoneNumberFormat_FailsOnPhoneNumber(string phoneNumber)
    {
        var command = new SendOtpCommand(phoneNumber);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(SendOtpCommand.PhoneNumber));
    }
}
