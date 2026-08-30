using Application.Wallet.Features.Commands.RequestWalletDebit;

namespace Tests.Application.Wallet.Features.Commands.RequestWalletDebit;

public class RequestWalletDebitValidatorTests
{
    private readonly RequestWalletDebitValidator _sut = new();

    private static RequestWalletDebitCommand ValidCommand(
        Guid? userId = null,
        decimal amount = 100_000m,
        string reason = "settlement",
        string? description = null,
        string idempotencyKey = "idem-123",
        int expiryHours = 72) =>
        new(userId ?? Guid.NewGuid(), amount, reason, description, idempotencyKey, expiryHours);

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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWalletDebitCommand.UserId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Validate_WithNonPositiveAmount_IsInvalid(decimal amount)
    {
        var result = _sut.Validate(ValidCommand(amount: amount));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWalletDebitCommand.Amount));
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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWalletDebitCommand.Amount));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingReason_IsInvalid(string reason)
    {
        var result = _sut.Validate(ValidCommand(reason: reason));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWalletDebitCommand.Reason));
    }

    [Fact]
    public void Validate_WithReasonAtMaximumLength_IsValid()
    {
        var reason = new string('r', 500);
        var result = _sut.Validate(ValidCommand(reason: reason));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithReasonLongerThanMaximum_IsInvalid()
    {
        var reason = new string('r', 501);
        var result = _sut.Validate(ValidCommand(reason: reason));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWalletDebitCommand.Reason));
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
        var description = new string('d', 1000);
        var result = _sut.Validate(ValidCommand(description: description));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithDescriptionLongerThanMaximum_IsInvalid()
    {
        var description = new string('d', 1001);
        var result = _sut.Validate(ValidCommand(description: description));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWalletDebitCommand.Description));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingIdempotencyKey_IsInvalid(string key)
    {
        var result = _sut.Validate(ValidCommand(idempotencyKey: key));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWalletDebitCommand.IdempotencyKey));
    }

    [Fact]
    public void Validate_WithIdempotencyKeyAtMaximumLength_IsValid()
    {
        var key = new string('k', 128);
        var result = _sut.Validate(ValidCommand(idempotencyKey: key));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithIdempotencyKeyLongerThanMaximum_IsInvalid()
    {
        var key = new string('k', 129);
        var result = _sut.Validate(ValidCommand(idempotencyKey: key));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWalletDebitCommand.IdempotencyKey));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(169)]
    [InlineData(1000)]
    public void Validate_WithExpiryHoursOutOfRange_IsInvalid(int hours)
    {
        var result = _sut.Validate(ValidCommand(expiryHours: hours));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestWalletDebitCommand.ExpiryHours));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(72)]
    [InlineData(168)]
    public void Validate_WithExpiryHoursAtBoundaries_IsValid(int hours)
    {
        var result = _sut.Validate(ValidCommand(expiryHours: hours));

        result.IsValid.ShouldBeTrue();
    }
}
