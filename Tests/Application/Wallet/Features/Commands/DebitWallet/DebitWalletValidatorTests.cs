using Application.Wallet.Features.Commands.DebitWallet;
using Domain.Wallet.Enums;

namespace Tests.Application.Wallet.Features.Commands.DebitWallet;

public class DebitWalletValidatorTests
{
    private readonly DebitWalletValidator _sut = new();

    private static DebitWalletCommand ValidCommand(
        Guid? userId = null,
        decimal amount = 10_000m,
        WalletTransactionType transactionType = WalletTransactionType.Debit,
        WalletReferenceType referenceType = WalletReferenceType.System,
        string idempotencyKey = "idem-123",
        string? correlationId = null,
        string? description = null,
        string? referenceId = null) =>
        new(
            userId ?? Guid.NewGuid(),
            amount,
            transactionType,
            referenceType,
            idempotencyKey,
            correlationId,
            description,
            referenceId);

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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DebitWalletCommand.UserId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500)]
    public void Validate_WithNonPositiveAmount_IsInvalid(decimal amount)
    {
        var result = _sut.Validate(ValidCommand(amount: amount));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DebitWalletCommand.Amount));
    }

    [Fact]
    public void Validate_WithAmountAboveMaximum_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(amount: 1_000_000_001m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DebitWalletCommand.Amount));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingIdempotencyKey_IsInvalid(string idempotencyKey)
    {
        var result = _sut.Validate(ValidCommand(idempotencyKey: idempotencyKey));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DebitWalletCommand.IdempotencyKey));
    }

    [Fact]
    public void Validate_WithIdempotencyKeyLongerThanMaximum_IsInvalid()
    {
        var longKey = new string('a', 129);
        var result = _sut.Validate(ValidCommand(idempotencyKey: longKey));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DebitWalletCommand.IdempotencyKey));
    }

    [Fact]
    public void Validate_WithInvalidTransactionType_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(transactionType: (WalletTransactionType)9999));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DebitWalletCommand.TransactionType));
    }

    [Fact]
    public void Validate_WithInvalidReferenceType_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(referenceType: (WalletReferenceType)9999));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DebitWalletCommand.ReferenceType));
    }

    [Fact]
    public void Validate_WithDescriptionLongerThanMaximum_IsInvalid()
    {
        var longDescription = new string('d', 1001);
        var result = _sut.Validate(ValidCommand(description: longDescription));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DebitWalletCommand.Description));
    }

    [Fact]
    public void Validate_WithNullOptionalFields_IsValid()
    {
        var result = _sut.Validate(ValidCommand(
            correlationId: null,
            description: null,
            referenceId: null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithCorrelationIdLongerThanMaximum_IsInvalid()
    {
        var longCorrelation = new string('c', 129);
        var result = _sut.Validate(ValidCommand(correlationId: longCorrelation));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DebitWalletCommand.CorrelationId));
    }
}
