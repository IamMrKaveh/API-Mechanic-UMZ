using Application.Wallet.Features.Commands.ReleaseWalletReservation;

namespace Tests.Application.Wallet.Features.Commands.ReleaseWalletReservation;

public class ReleaseWalletReservationValidatorTests
{
    private readonly ReleaseWalletReservationValidator _sut = new();

    private static ReleaseWalletReservationCommand ValidCommand(
        Guid? userId = null,
        Guid? walletReservationId = null) =>
        new(userId ?? Guid.NewGuid(), walletReservationId ?? Guid.NewGuid());

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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReleaseWalletReservationCommand.UserId));
    }

    [Fact]
    public void Validate_WithEmptyWalletReservationId_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(walletReservationId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReleaseWalletReservationCommand.WalletReservationId));
    }

    [Fact]
    public void Validate_WithBothIdsEmpty_ReturnsErrorForBoth()
    {
        var result = _sut.Validate(new ReleaseWalletReservationCommand(Guid.Empty, Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReleaseWalletReservationCommand.UserId));
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReleaseWalletReservationCommand.WalletReservationId));
    }
}
