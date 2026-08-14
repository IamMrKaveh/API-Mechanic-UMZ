using Application.Wallet.Features.Commands.CreditWallet;
using Domain.Wallet.Enums;

namespace Tests.Application.Wallet.Features.Commands.CreditWallet;

public class CreditWalletValidatorTests
{
    private readonly CreditWalletValidator _sut = new();

    private static CreditWalletCommand ValidCommand(
        Guid? userId = null,
        decimal amount = 100_000m,
        WalletTransactionType transactionType = WalletTransactionType.Credit,
        WalletReferenceType referenceType = WalletReferenceType.System,
        string referenceId = "ref-123",
        string idempotencyKey = "idem-123",
        string? correlationId = null,
        string? description = null) =>
        new(
            userId ?? Guid.NewGuid(),
            amount,
            transactionType,
            referenceType,
            referenceId,
            idempotencyKey,
            correlationId,
            description);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyUserId_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(userId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreditWalletCommand.UserId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100_000)]
    public void Validate_WithNonPositiveAmount_IsInvalid(decimal amount)
    {
        var result = _sut.Validate(ValidCommand(amount: amount));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreditWalletCommand.Amount));
    }

    [Fact]
    public void Validate_WithAmountAboveMaximum_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(amount: 1_000_000_001m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreditWalletCommand.Amount));
    }

    [Fact]
    public void Validate_WithAmountAtMaximum_IsValid()
    {
        var result = _sut.Validate(ValidCommand(amount: 1_000_000_000m));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingIdempotencyKey_IsInvalid(string idempotencyKey)
    {
        var result = _sut.Validate(ValidCommand(idempotencyKey: idempotencyKey));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreditWalletCommand.IdempotencyKey));
    }

    [Fact]
    public void Validate_WithIdempotencyKeyLongerThanMaximum_IsInvalid()
    {
        var longKey = new string('a', 129);
        var result = _sut.Validate(ValidCommand(idempotencyKey: longKey));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreditWalletCommand.IdempotencyKey));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingReferenceId_IsInvalid(string referenceId)
    {
        var result = _sut.Validate(ValidCommand(referenceId: referenceId));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreditWalletCommand.ReferenceId));
    }

    [Fact]
    public void Validate_WithReferenceIdLongerThanMaximum_IsInvalid()
    {
        var longRef = new string('r', 257);
        var result = _sut.Validate(ValidCommand(referenceId: longRef));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreditWalletCommand.ReferenceId));
    }

    [Fact]
    public void Validate_WithInvalidTransactionType_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(transactionType: (WalletTransactionType)9999));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreditWalletCommand.TransactionType));
    }

    [Fact]
    public void Validate_WithInvalidReferenceType_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(referenceType: (WalletReferenceType)9999));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreditWalletCommand.ReferenceType));
    }

    [Fact]
    public void Validate_WithDescriptionLongerThanMaximum_IsInvalid()
    {
        var longDescription = new string('d', 1001);
        var result = _sut.Validate(ValidCommand(description: longDescription));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreditWalletCommand.Description));
    }

    [Fact]
    public void Validate_WithNullDescription_IsValid()
    {
        var result = _sut.Validate(ValidCommand(description: null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithCorrelationIdLongerThanMaximum_IsInvalid()
    {
        var longCorrelation = new string('c', 129);
        var result = _sut.Validate(ValidCommand(correlationId: longCorrelation));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreditWalletCommand.CorrelationId));
    }
}
