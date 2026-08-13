using Application.Auth.Features.Commands.VerifyOtp;

namespace Tests.Application.Auth.Features.Commands.VerifyOtp;

public class VerifyOtpValidatorTests
{
    private readonly VerifyOtpValidator _sut = new();

    [Fact]
    public void Validate_WithValidPhoneAndCode_IsValid()
    {
        var command = new VerifyOtpCommand("09123456789", "1234");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyPhoneNumber_FailsOnPhoneNumber(string phoneNumber)
    {
        var command = new VerifyOtpCommand(phoneNumber, "123456");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(VerifyOtpCommand.PhoneNumber));
    }

    [Theory]
    [InlineData("0912345678")]
    [InlineData("091234567890")]
    [InlineData("08123456789")]
    public void Validate_WithInvalidPhoneNumberFormat_FailsOnPhoneNumber(string phoneNumber)
    {
        var command = new VerifyOtpCommand(phoneNumber, "123456");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(VerifyOtpCommand.PhoneNumber));
    }

    [Fact]
    public void Validate_WithEmptyCode_FailsOnCode()
    {
        var command = new VerifyOtpCommand("09123456789", string.Empty);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(VerifyOtpCommand.Code));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456789")]
    public void Validate_WithCodeLengthOutsideAllowedRange_FailsOnCode(string code)
    {
        var command = new VerifyOtpCommand("09123456789", code);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(VerifyOtpCommand.Code));
    }

    [Theory]
    [InlineData("12ab")]
    [InlineData("abcdef")]
    [InlineData("12 45")]
    public void Validate_WithNonDigitCode_FailsOnCode(string code)
    {
        var command = new VerifyOtpCommand("09123456789", code);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(VerifyOtpCommand.Code));
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("12345")]
    [InlineData("123456")]
    [InlineData("1234567")]
    [InlineData("12345678")]
    public void Validate_WithDigitOnlyCodeWithinAllowedLength_IsValid(string code)
    {
        var command = new VerifyOtpCommand("09123456789", code);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
