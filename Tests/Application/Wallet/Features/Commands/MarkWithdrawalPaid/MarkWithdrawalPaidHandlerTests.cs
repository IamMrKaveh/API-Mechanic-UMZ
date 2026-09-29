using Application.Wallet.Features.Commands.MarkWithdrawalPaid;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.MarkWithdrawalPaid;

public sealed class MarkWithdrawalPaidHandlerTests : HandlerTestBase
{
    private readonly IWalletWithdrawalRepository _withdrawalRepository = Substitute.For<IWalletWithdrawalRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly MarkWithdrawalPaidHandler _sut;

    public MarkWithdrawalPaidHandlerTests()
    {
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FakeLockHandle("wallet", true));

        _sut = new MarkWithdrawalPaidHandler(
            _withdrawalRepository, _walletRepository,
            _distributedLock, AuditService, DateTimeProvider, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenWithdrawalNotFound_ReturnsNotFound()
    {
        CurrentUserService.UserId.Returns(Guid.NewGuid());
        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns((WalletWithdrawalRequest?)null);

        var result = await _sut.Handle(new MarkWithdrawalPaidCommand(Guid.NewGuid(), "REF-1"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenLockNotAcquired_ReturnsConflict()
    {
        var adminId = UserId.NewId();
        CurrentUserService.UserId.Returns(adminId.Value);
        var withdrawal = new WalletWithdrawalRequestBuilder().Build();
        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((ILockHandle?)null);

        var result = await _sut.Handle(new MarkWithdrawalPaidCommand(withdrawal.Id.Value, "REF-1"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenWalletNotFound_ReturnsNotFound()
    {
        var adminId = UserId.NewId();
        CurrentUserService.UserId.Returns(adminId.Value);
        var withdrawal = new WalletWithdrawalRequestBuilder().Build();
        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(new MarkWithdrawalPaidCommand(withdrawal.Id.Value, "REF-1"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenValidApprovedWithdrawal_MarksPaidAndDebitsWallet()
    {
        var adminId = UserId.NewId();
        var userId = UserId.NewId();
        CurrentUserService.UserId.Returns(adminId.Value);

        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), DateTime.UtcNow, Guid.NewGuid().ToString("N"));
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Money.Create(200_000m), "withdrawal-request", DateTime.UtcNow);

        var withdrawal = new WalletWithdrawalRequestBuilder()
            .WithUserId(userId)
            .WithAmount(200_000m)
            .WithReservationId(reservationId)
            .Build();
        withdrawal.Approve(adminId, DateTime.UtcNow);

        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);
        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(new MarkWithdrawalPaidCommand(withdrawal.Id.Value, "REF-XYZ"), CancellationToken.None);

        result.ShouldBeSuccess();
        withdrawal.Status.ShouldBe(WalletWithdrawalStatus.Paid);
        withdrawal.BankReferenceNumber.ShouldBe("REF-XYZ");
        wallet.Balance.Amount.ShouldBe(300_000m);
        await AuditService.Received(1).LogSystemEventAsync(
            "WithdrawalMarkedPaid", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
