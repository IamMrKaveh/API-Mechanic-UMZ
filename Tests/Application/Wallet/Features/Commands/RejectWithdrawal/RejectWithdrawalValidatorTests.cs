using Application.Wallet.Features.Commands.RejectWithdrawal;

namespace Tests.Application.Wallet.Features.Commands.RejectWithdrawal;

public class RejectWithdrawalValidatorTests
{
    private readonly RejectWithdrawalValidator _sut = new();

    private static RejectWithdrawalCommand ValidCommand(
        Guid? withdrawalId = null,
        string reason = "insufficient documents") =>
        new(withdrawalId ?? Guid.NewGuid(), reason);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyWithdrawalId_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(withdrawalId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RejectWithdrawalCommand.WithdrawalId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingReason_IsInvalid(string reason)
    {
        var result = _sut.Validate(ValidCommand(reason: reason));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RejectWithdrawalCommand.Reason));
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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RejectWithdrawalCommand.Reason));
    }
}
