using Application.Wallet.Features.Commands.ReserveWallet;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Application.Wallet.Features.Commands.ReserveWallet;

public class ReserveWalletValidatorTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 08, 30, 12, 0, 0, DateTimeKind.Utc);

    private readonly ReserveWalletValidator _sut = new(new FixedDateTimeProvider(FixedUtcNow));

    private static ReserveWalletCommand ValidCommand(
        Guid? userId = null,
        decimal amount = 100_000m,
        Guid? walletId = null,
        DateTime? expiresAt = null) =>
        new(userId ?? Guid.NewGuid(), amount, walletId ?? Guid.NewGuid(), expiresAt);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNullExpiresAt_IsValid()
    {
        var result = _sut.Validate(ValidCommand(expiresAt: null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFutureExpiresAt_IsValid()
    {
        var result = _sut.Validate(ValidCommand(expiresAt: FixedUtcNow.AddHours(1)));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyUserId_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(userId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReserveWalletCommand.UserId));
    }

    [Fact]
    public void Validate_WithEmptyWalletId_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(walletId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReserveWalletCommand.WalletId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100_000)]
    public void Validate_WithNonPositiveAmount_IsInvalid(decimal amount)
    {
        var result = _sut.Validate(ValidCommand(amount: amount));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReserveWalletCommand.Amount));
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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReserveWalletCommand.Amount));
    }

    [Fact]
    public void Validate_WithExpiresAtEqualToNow_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(expiresAt: FixedUtcNow));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReserveWalletCommand.ExpiresAt));
    }

    [Fact]
    public void Validate_WithExpiresAtInPast_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(expiresAt: FixedUtcNow.AddSeconds(-1)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReserveWalletCommand.ExpiresAt));
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow);
    }
}
