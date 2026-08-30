using Application.Wallet.Features.Commands.ConfirmWalletTransfer;

namespace Tests.Application.Wallet.Features.Commands.ConfirmWalletTransfer;

public class ConfirmWalletTransferValidatorTests
{
    private readonly ConfirmWalletTransferValidator _sut = new();

    private static ConfirmWalletTransferCommand ValidCommand(
        Guid? transferId = null,
        string otpCode = "123456") =>
        new(transferId ?? Guid.NewGuid(), otpCode);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyTransferId_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(transferId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ConfirmWalletTransferCommand.TransferId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingOtpCode_IsInvalid(string otpCode)
    {
        var result = _sut.Validate(ValidCommand(otpCode: otpCode));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ConfirmWalletTransferCommand.OtpCode));
    }

    [Theory]
    [InlineData("abcd")]
    [InlineData("12a4")]
    [InlineData("12-34")]
    [InlineData(" 1234")]
    public void Validate_WithNonNumericOtpCode_IsInvalid(string otpCode)
    {
        var result = _sut.Validate(ValidCommand(otpCode: otpCode));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ConfirmWalletTransferCommand.OtpCode));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("123")]
    [InlineData("123456789")]
    public void Validate_WithOtpCodeOutsideAllowedLength_IsInvalid(string otpCode)
    {
        var result = _sut.Validate(ValidCommand(otpCode: otpCode));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ConfirmWalletTransferCommand.OtpCode));
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("12345")]
    [InlineData("123456")]
    [InlineData("1234567")]
    [InlineData("12345678")]
    public void Validate_WithOtpCodeAtAllowedLengths_IsValid(string otpCode)
    {
        var result = _sut.Validate(ValidCommand(otpCode: otpCode));

        result.IsValid.ShouldBeTrue();
    }
}
