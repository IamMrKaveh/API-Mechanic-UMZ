using Application.Wallet.Features.Commands.ReleaseWalletReservation;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.ReleaseWalletReservation;

public sealed class ReleaseWalletReservationHandlerTests : HandlerTestBase
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly ReleaseWalletReservationHandler _sut;

    public ReleaseWalletReservationHandlerTests()
    {

        _sut = new ReleaseWalletReservationHandler(_walletRepository, DateTimeProvider, AuditService);
    }

    [Fact]
    public async Task Handle_WhenWalletNotFound_ReturnsSuccessIdempotently()
    {
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(
            new ReleaseWalletReservationCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.ShouldBeSuccess();
        _walletRepository.DidNotReceive().Update(Arg.Any<Wallets>());
    }

    [Fact]
    public async Task Handle_WhenReservationExists_ReleasesAndReturnsSuccess()
    {
        var userId = UserId.NewId();
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), DateTime.UtcNow, Guid.NewGuid().ToString("N"));
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Money.Create(200_000m), "test", DateTime.UtcNow);

        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(
            new ReleaseWalletReservationCommand(userId.Value, reservationId.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.AvailableBalance.Amount.ShouldBe(500_000m);
        _walletRepository.Received(1).Update(wallet);
        await UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenReservationDoesNotExist_ReturnsSuccessWithoutSideEffects()
    {
        var userId = UserId.NewId();
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();

        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(
            new ReleaseWalletReservationCommand(userId.Value, Guid.NewGuid()), CancellationToken.None);

        result.ShouldBeSuccess();
    }
}
