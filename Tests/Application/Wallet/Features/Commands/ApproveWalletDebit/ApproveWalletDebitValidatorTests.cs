using Application.Wallet.Features.Commands.ApproveWalletDebit;

namespace Tests.Application.Wallet.Features.Commands.ApproveWalletDebit;

public class ApproveWalletDebitValidatorTests
{
    private readonly ApproveWalletDebitValidator _sut = new();

    private static ApproveWalletDebitCommand ValidCommand(Guid? requestId = null) =>
        new(requestId ?? Guid.NewGuid());

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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ApproveWalletDebitCommand.RequestId));
    }
}
