using Application.Wallet.Features.Commands.InitiateWalletTopUp;

namespace Tests.Application.Wallet.Features.Commands.InitiateWalletTopUp;

public class InitiateWalletTopUpValidatorTests
{
    private readonly InitiateWalletTopUpValidator _sut = new();

    private static InitiateWalletTopUpCommand ValidCommand(
        decimal amount = 50_000m,
        string gateway = "zarinpal") =>
        new(amount, gateway);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9_999)]
    public void Validate_WithAmountBelowMinimum_IsInvalid(decimal amount)
    {
        var result = _sut.Validate(ValidCommand(amount: amount));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(InitiateWalletTopUpCommand.Amount));
    }

    [Fact]
    public void Validate_WithAmountAtMinimum_IsValid()
    {
        var result = _sut.Validate(ValidCommand(amount: 10_000m));

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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(InitiateWalletTopUpCommand.Amount));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingGateway_IsInvalid(string gateway)
    {
        var result = _sut.Validate(ValidCommand(gateway: gateway));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(InitiateWalletTopUpCommand.Gateway));
    }

    [Fact]
    public void Validate_WithGatewayLongerThanMaximum_IsInvalid()
    {
        var gateway = new string('g', 65);
        var result = _sut.Validate(ValidCommand(gateway: gateway));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(InitiateWalletTopUpCommand.Gateway));
    }

    [Fact]
    public void Validate_WithGatewayAtMaximumLength_IsValid()
    {
        var gateway = new string('g', 64);
        var result = _sut.Validate(ValidCommand(gateway: gateway));

        result.IsValid.ShouldBeTrue();
    }
}
