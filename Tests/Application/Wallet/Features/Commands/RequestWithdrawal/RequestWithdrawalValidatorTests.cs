using Application.Wallet.Features.Commands.RequestWithdrawal;

namespace Tests.Application.Wallet.Features.Commands.RequestWithdrawal;

public class RequestWithdrawalValidatorTests
{
    private const string ValidIban = "IR820540102680020817909002";

    private readonly RequestWithdrawalValidator _sut = new();

    private static RequestWithdrawalCommand ValidCommand(
        decimal amount = 100_000m,
        string iban = ValidIban,
        string accountHolder = "Ali Ahmadi",
        string? description = null) =>
        new(amount, iban, accountHolder, description);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100_000)]
    public void Validate_WithNonPositiveAmount_IsInvalid(decimal amount)
    {
        var result = _sut.Validate(ValidCommand(amount: amount));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWithdrawalCommand.Amount));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1_000)]
    [InlineData(49_999)]
    public void Validate_WithAmountBelowMinimum_IsInvalid(decimal amount)
    {
        var result = _sut.Validate(ValidCommand(amount: amount));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWithdrawalCommand.Amount));
    }

    [Fact]
    public void Validate_WithAmountAtMinimum_IsValid()
    {
        var result = _sut.Validate(ValidCommand(amount: 50_000m));

        result.IsValid.ShouldBeTrue();
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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWithdrawalCommand.Amount));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingIban_IsInvalid(string iban)
    {
        var result = _sut.Validate(ValidCommand(iban: iban));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWithdrawalCommand.Iban));
    }

    [Theory]
    [InlineData("US820540102680020817909002")]
    [InlineData("IR12345")]
    [InlineData("IR8205401026800208179090021234")]
    [InlineData("IRABCDEFGHIJKLMNOPQRSTUVWX")]
    public void Validate_WithMalformedIban_IsInvalid(string iban)
    {
        var result = _sut.Validate(ValidCommand(iban: iban));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWithdrawalCommand.Iban));
    }

    [Fact]
    public void Validate_WithInvalidIbanChecksum_IsInvalid()
    {
        var iban = "IR000000000000000000000000";
        var result = _sut.Validate(ValidCommand(iban: iban));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWithdrawalCommand.Iban));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingAccountHolder_IsInvalid(string accountHolder)
    {
        var result = _sut.Validate(ValidCommand(accountHolder: accountHolder));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWithdrawalCommand.AccountHolder));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("  A ")]
    public void Validate_WithAccountHolderShorterThanMinimum_IsInvalid(string accountHolder)
    {
        var result = _sut.Validate(ValidCommand(accountHolder: accountHolder));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWithdrawalCommand.AccountHolder));
    }

    [Fact]
    public void Validate_WithAccountHolderAtMinimumLength_IsValid()
    {
        var result = _sut.Validate(ValidCommand(accountHolder: "Ali"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithAccountHolderAtMaximumLength_IsValid()
    {
        var accountHolder = new string('a', 150);
        var result = _sut.Validate(ValidCommand(accountHolder: accountHolder));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithAccountHolderLongerThanMaximum_IsInvalid()
    {
        var accountHolder = new string('a', 151);
        var result = _sut.Validate(ValidCommand(accountHolder: accountHolder));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWithdrawalCommand.AccountHolder));
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
        var description = new string('d', 500);
        var result = _sut.Validate(ValidCommand(description: description));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithDescriptionLongerThanMaximum_IsInvalid()
    {
        var description = new string('d', 501);
        var result = _sut.Validate(ValidCommand(description: description));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWithdrawalCommand.Description));
    }
}
