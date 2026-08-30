using Application.Wallet.Features.Commands.InitiateWalletTransfer;

namespace Tests.Application.Wallet.Features.Commands.InitiateWalletTransfer;

public class InitiateWalletTransferValidatorTests
{
    private readonly InitiateWalletTransferValidator _sut = new();

    private static InitiateWalletTransferCommand ValidCommand(
        string recipientPhoneNumber = "09121234567",
        decimal amount = 50_000m,
        string? description = "hello") =>
        new(recipientPhoneNumber, amount, description);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingRecipientPhoneNumber_IsInvalid(string phone)
    {
        var result = _sut.Validate(ValidCommand(recipientPhoneNumber: phone));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(InitiateWalletTransferCommand.RecipientPhoneNumber));
    }

    [Fact]
    public void Validate_WithRecipientPhoneNumberLongerThanMaximum_IsInvalid()
    {
        var phone = new string('9', 33);
        var result = _sut.Validate(ValidCommand(recipientPhoneNumber: phone));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(InitiateWalletTransferCommand.RecipientPhoneNumber));
    }

    [Fact]
    public void Validate_WithRecipientPhoneNumberAtMaximumLength_IsValid()
    {
        var phone = new string('9', 32);
        var result = _sut.Validate(ValidCommand(recipientPhoneNumber: phone));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithNonPositiveAmount_IsInvalid(decimal amount)
    {
        var result = _sut.Validate(ValidCommand(amount: amount));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(InitiateWalletTransferCommand.Amount));
    }

    [Fact]
    public void Validate_WithAmountAtMaximum_IsValid()
    {
        var result = _sut.Validate(ValidCommand(amount: 1_000_000_000m));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithAmountAboveMaximum_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(amount: 1_000_000_001m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(InitiateWalletTransferCommand.Amount));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrWhitespaceDescription_IsValid(string? description)
    {
        var result = _sut.Validate(ValidCommand(description: description));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithDescriptionAtMaximumLength_IsValid()
    {
        var description = new string('x', 500);
        var result = _sut.Validate(ValidCommand(description: description));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithDescriptionLongerThanMaximum_IsInvalid()
    {
        var description = new string('x', 501);
        var result = _sut.Validate(ValidCommand(description: description));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(InitiateWalletTransferCommand.Description));
    }
}
