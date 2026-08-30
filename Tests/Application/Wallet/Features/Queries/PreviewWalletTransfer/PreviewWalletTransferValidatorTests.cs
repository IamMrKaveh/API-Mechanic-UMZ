using Application.Wallet.Features.Queries.PreviewWalletTransfer;

namespace Tests.Application.Wallet.Features.Queries.PreviewWalletTransfer;

public class PreviewWalletTransferValidatorTests
{
    private readonly PreviewWalletTransferValidator _sut = new();

    private static PreviewWalletTransferQuery Query(
        string recipientPhoneNumber = "09123456789",
        decimal amount = 100_000m) =>
        new(recipientPhoneNumber, amount);

    [Fact]
    public void Validate_WithValidQuery_IsValid()
    {
        var result = _sut.Validate(Query());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyOrWhitespaceRecipientPhoneNumber_IsInvalid(string recipientPhoneNumber)
    {
        var result = _sut.Validate(Query(recipientPhoneNumber: recipientPhoneNumber));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(PreviewWalletTransferQuery.RecipientPhoneNumber)
            && e.ErrorMessage == "شماره موبایل گیرنده الزامی است.");
    }

    [Fact]
    public void Validate_WithRecipientPhoneNumberAtMaximumLength_IsValid()
    {
        var recipientPhoneNumber = new string('9', 32);

        var result = _sut.Validate(Query(recipientPhoneNumber: recipientPhoneNumber));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithRecipientPhoneNumberLongerThanMaximumLength_IsInvalid()
    {
        var recipientPhoneNumber = new string('9', 33);

        var result = _sut.Validate(Query(recipientPhoneNumber: recipientPhoneNumber));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PreviewWalletTransferQuery.RecipientPhoneNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Validate_WithAmountNotGreaterThanZero_IsInvalid(decimal amount)
    {
        var result = _sut.Validate(Query(amount: amount));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(PreviewWalletTransferQuery.Amount)
            && e.ErrorMessage == "مبلغ باید بزرگتر از صفر باشد.");
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(1_000)]
    [InlineData(100_000)]
    [InlineData(1_000_000_000)]
    public void Validate_WithAmountWithinAllowedRange_IsValid(decimal amount)
    {
        var result = _sut.Validate(Query(amount: amount));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1_000_000_000.01)]
    [InlineData(2_000_000_000)]
    public void Validate_WithAmountAboveMaximum_IsInvalid(decimal amount)
    {
        var result = _sut.Validate(Query(amount: amount));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(PreviewWalletTransferQuery.Amount)
            && e.ErrorMessage == "مبلغ از سقف مجاز عبور کرده است.");
    }

    [Fact]
    public void Validate_WithEmptyPhoneAndInvalidAmount_ReportsBothErrors()
    {
        var result = _sut.Validate(new PreviewWalletTransferQuery(string.Empty, 0m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PreviewWalletTransferQuery.RecipientPhoneNumber));
        result.Errors.ShouldContain(e => e.PropertyName == nameof(PreviewWalletTransferQuery.Amount));
    }
}
