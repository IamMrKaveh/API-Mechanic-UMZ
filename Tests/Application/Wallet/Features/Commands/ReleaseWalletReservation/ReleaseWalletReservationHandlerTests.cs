using Application.Wallet.Features.Commands.ReleaseWalletReservation;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.ReleaseWalletReservation;

public sealed class ReleaseWalletReservationHandlerTests
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    private readonly ReleaseWalletReservationHandler _sut;

    public ReleaseWalletReservationHandlerTests()
    {
        _sut = new ReleaseWalletReservationHandler(_walletRepository, _unitOfWork, _auditService);
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
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Money.Create(200_000m), "test");

        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(
            new ReleaseWalletReservationCommand(userId.Value, reservationId.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.AvailableBalance.Amount.ShouldBe(500_000m);
        _walletRepository.Received(1).Update(wallet);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

    [Fact]
    public async Task Handle_WhenConcurrencyExceptionThrown_ReturnsConflict()
    {
        var userId = UserId.NewId();
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Money.Create(200_000m), "test");

        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new ConcurrencyException());

        var result = await _sut.Handle(
            new ReleaseWalletReservationCommand(userId.Value, reservationId.Value), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
        await _auditService.Received().LogSystemEventAsync(
            "WalletReleaseConcurrencyConflict", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
