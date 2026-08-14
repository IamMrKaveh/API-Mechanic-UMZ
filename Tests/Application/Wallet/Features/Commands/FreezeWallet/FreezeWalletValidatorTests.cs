using Application.Wallet.Features.Commands.FreezeWallet;

namespace Tests.Application.Wallet.Features.Commands.FreezeWallet;

public class FreezeWalletValidatorTests { private readonly FreezeWalletValidator _sut = new();

private static FreezeWalletCommand ValidCommand(
    Guid? userId = null,
    string reason = "compliance-hold") =>
    new(userId ?? Guid.NewGuid(), reason);

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
    result.Errors.ShouldContain(e => e.PropertyName == nameof(FreezeWalletCommand.UserId));
}

[Theory]
[InlineData("")]
[InlineData("   ")]
public void Validate_WithMissingReason_IsInvalid(string reason)
{
    var result = _sut.Validate(ValidCommand(reason: reason));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(FreezeWalletCommand.Reason));
}

[Fact]
public void Validate_WithReasonLongerThanMaximum_IsInvalid()
{
    var longReason = new string('r', 501);
    var result = _sut.Validate(ValidCommand(reason: longReason));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(FreezeWalletCommand.Reason));
}

[Fact]
public void Validate_WithReasonAtMaximumLength_IsValid()
{
    var reason = new string('r', 500);
    var result = _sut.Validate(ValidCommand(reason: reason));

    result.IsValid.ShouldBeTrue();
}

}