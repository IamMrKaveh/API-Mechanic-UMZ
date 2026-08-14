using Application.Wallet.Features.Commands.UnfreezeWallet;

namespace Tests.Application.Wallet.Features.Commands.UnfreezeWallet;

public class UnfreezeWalletValidatorTests { private readonly UnfreezeWalletValidator _sut = new();

[Fact]
public void Validate_WithValidCommand_IsValid()
{
    var result = _sut.Validate(new UnfreezeWalletCommand(Guid.NewGuid()));

    result.IsValid.ShouldBeTrue();
}

[Fact]
public void Validate_WithEmptyUserId_IsInvalid()
{
    var result = _sut.Validate(new UnfreezeWalletCommand(Guid.Empty));

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UnfreezeWalletCommand.UserId));
}

}