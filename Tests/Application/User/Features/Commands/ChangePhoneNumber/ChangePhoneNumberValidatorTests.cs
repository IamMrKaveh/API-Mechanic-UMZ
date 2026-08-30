using Application.User.Features.Commands.ChangePhoneNumber;

namespace Tests.Application.User.Features.Commands.ChangePhoneNumber;

public class ChangePhoneNumberValidatorTests
{
    private readonly ChangePhoneNumberValidator _sut = new();

    private static ChangePhoneNumberCommand ValidCommand(
        string newPhoneNumber = "09123456789",
        string otpCode = "123456") =>
        new(newPhoneNumber, otpCode);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("09123456789")]
    [InlineData("+989123456789")]
    [InlineData("00989123456789")]
    [InlineData("989123456789")]
    [InlineData("9123456789")]
    public void Validate_WithAcceptedPhoneNumberFormats_IsValid(string phoneNumber)
    {
        var result = _sut.Validate(ValidCommand(newPhoneNumber: phoneNumber));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyNewPhoneNumber_FailsOnNewPhoneNumber()
    {
        var result = _sut.Validate(ValidCommand(newPhoneNumber: string.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePhoneNumberCommand.NewPhoneNumber));
    }

    [Fact]
    public void Validate_WithNullNewPhoneNumber_FailsOnNewPhoneNumber()
    {
        var result = _sut.Validate(ValidCommand(newPhoneNumber: null!));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePhoneNumberCommand.NewPhoneNumber));
    }

    [Theory]
    [InlineData("0912345678")]
    [InlineData("091234567890")]
    [InlineData("08123456789")]
    [InlineData("00123456789")]
    [InlineData("12345678901")]
    [InlineData("abcdefghijk")]
    [InlineData("0912-345-6789")]
    [InlineData("+98 912 345 6789")]
    [InlineData("+981234567890")]
    public void Validate_WithInvalidPhoneNumberFormat_FailsOnNewPhoneNumber(string phoneNumber)
    {
        var result = _sut.Validate(ValidCommand(newPhoneNumber: phoneNumber));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePhoneNumberCommand.NewPhoneNumber));
    }

    [Fact]
    public void Validate_WithEmptyOtpCode_FailsOnOtpCode()
    {
        var result = _sut.Validate(ValidCommand(otpCode: string.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePhoneNumberCommand.OtpCode));
    }

    [Fact]
    public void Validate_WithNullOtpCode_FailsOnOtpCode()
    {
        var result = _sut.Validate(ValidCommand(otpCode: null!));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePhoneNumberCommand.OtpCode));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12345678")]
    public void Validate_WithOtpCodeLengthDifferentFromSix_FailsOnOtpCode(string otpCode)
    {
        var result = _sut.Validate(ValidCommand(otpCode: otpCode));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePhoneNumberCommand.OtpCode));
    }

    [Fact]
    public void Validate_WithOtpCodeExactlySixCharacters_IsValid()
    {
        var result = _sut.Validate(ValidCommand(otpCode: "654321"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithBothFieldsInvalid_ReturnsErrorsForBoth()
    {
        var result = _sut.Validate(new ChangePhoneNumberCommand(string.Empty, string.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePhoneNumberCommand.NewPhoneNumber));
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePhoneNumberCommand.OtpCode));
    }
}
