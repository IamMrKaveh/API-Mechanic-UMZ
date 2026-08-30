using Application.Wallet.Features.Commands.MarkWithdrawalPaid;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.MarkWithdrawalPaid;

public sealed class MarkWithdrawalPaidHandlerTests
{
    private readonly IWalletWithdrawalRepository _withdrawalRepository = Substitute.For<IWalletWithdrawalRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly MarkWithdrawalPaidHandler _sut;

    public MarkWithdrawalPaidHandlerTests()
    {
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FakeLockHandle("wallet", true));

        _sut = new MarkWithdrawalPaidHandler(
            _withdrawalRepository, _walletRepository, _unitOfWork,
            _distributedLock, _auditService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenWithdrawalNotFound_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns((WalletWithdrawalRequest?)null);

        var result = await _sut.Handle(new MarkWithdrawalPaidCommand(Guid.NewGuid(), "REF-1"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenLockNotAcquired_ReturnsConflict()
    {
        var adminId = UserId.NewId();
        _currentUserService.UserId.Returns(adminId.Value);
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
        _currentUserService.UserId.Returns(adminId.Value);
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
        _currentUserService.UserId.Returns(adminId.Value);

        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Money.Create(200_000m), "withdrawal-request");

        var withdrawal = new WalletWithdrawalRequestBuilder()
            .WithUserId(userId)
            .WithAmount(200_000m)
            .WithReservationId(reservationId)
            .Build();
        withdrawal.Approve(adminId);

        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);
        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(new MarkWithdrawalPaidCommand(withdrawal.Id.Value, "REF-XYZ"), CancellationToken.None);

        result.ShouldBeSuccess();
        withdrawal.Status.ShouldBe(WalletWithdrawalStatus.Paid);
        withdrawal.BankReferenceNumber.ShouldBe("REF-XYZ");
        wallet.Balance.Amount.ShouldBe(300_000m);
        await _auditService.Received(1).LogSystemEventAsync(
            "WithdrawalMarkedPaid", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenConcurrencyExceptionThrown_ReturnsConflict()
    {
        var adminId = UserId.NewId();
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(adminId.Value);

        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Money.Create(200_000m), "withdrawal-request");

        var withdrawal = new WalletWithdrawalRequestBuilder()
            .WithUserId(userId).WithAmount(200_000m).WithReservationId(reservationId).Build();

        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);
        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new ConcurrencyException());

        var result = await _sut.Handle(new MarkWithdrawalPaidCommand(withdrawal.Id.Value, "REF-1"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
        await _auditService.Received().LogSystemEventAsync(
            "WithdrawalMarkPaidConcurrencyConflict", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
