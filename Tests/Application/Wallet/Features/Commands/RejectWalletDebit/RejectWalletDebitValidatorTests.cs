using Application.Wallet.Features.Commands.RejectWalletDebit;

namespace Tests.Application.Wallet.Features.Commands.RejectWalletDebit;

public class RejectWalletDebitValidatorTests
{
    private readonly RejectWalletDebitValidator _sut = new();

    private static RejectWalletDebitCommand ValidCommand(
        Guid? requestId = null,
        string reason = "user rejected") =>
        new(requestId ?? Guid.NewGuid(), reason);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyRequestId_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(requestId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RejectWalletDebitCommand.RequestId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingReason_IsInvalid(string reason)
    {
        var result = _sut.Validate(ValidCommand(reason: reason));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RejectWalletDebitCommand.RejectionReason));
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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RejectWalletDebitCommand.RejectionReason));
    }
}
