using Application.Wallet.Features.Commands.CancelWithdrawal;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.CancelWithdrawal;

public sealed class CancelWithdrawalHandlerTests
{
    private readonly IWalletWithdrawalRepository _withdrawalRepository = Substitute.For<IWalletWithdrawalRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly CancelWithdrawalHandler _sut;

    public CancelWithdrawalHandlerTests()
    {
        _sut = new CancelWithdrawalHandler(_withdrawalRepository, _walletRepository, _auditService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenWithdrawalNotFound_ReturnsNotFound()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns((WalletWithdrawalRequest?)null);

        var result = await _sut.Handle(new CancelWithdrawalCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwner_ReturnsFailure()
    {
        var ownerId = UserId.NewId();
        var otherUserId = UserId.NewId();
        _currentUserService.UserId.Returns(otherUserId.Value);
        var withdrawal = new WalletWithdrawalRequestBuilder().WithUserId(ownerId).Build();
        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);

        var result = await _sut.Handle(new CancelWithdrawalCommand(withdrawal.Id.Value), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenWalletNotFound_ReturnsNotFound()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        var withdrawal = new WalletWithdrawalRequestBuilder().WithUserId(userId).Build();
        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(new CancelWithdrawalCommand(withdrawal.Id.Value), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenValid_ReleasesReservationCancelsWithdrawalAndReturnsSuccess()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);

        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Money.Create(100_000m), "withdrawal-request");

        var withdrawal = new WalletWithdrawalRequestBuilder()
            .WithUserId(userId)
            .WithAmount(100_000m)
            .WithReservationId(reservationId)
            .Build();

        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(new CancelWithdrawalCommand(withdrawal.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        withdrawal.Status.ShouldBe(WalletWithdrawalStatus.Cancelled);
        wallet.AvailableBalance.Amount.ShouldBe(500_000m);
        _walletRepository.Received(1).Update(wallet);
        _withdrawalRepository.Received(1).Update(withdrawal);
        await _auditService.Received(1).LogSecurityEventAsync(
            "WithdrawalCancelled",
            Arg.Any<string>(),
            Arg.Any<IpAddress>(),
            Arg.Any<UserId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWithdrawalNotPending_ReturnsFailure()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);

        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Money.Create(100_000m), "withdrawal-request");

        var withdrawal = new WalletWithdrawalRequestBuilder()
            .WithUserId(userId)
            .WithAmount(100_000m)
            .WithReservationId(reservationId)
            .Build();
        withdrawal.Approve(userId);

        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(new CancelWithdrawalCommand(withdrawal.Id.Value), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}
