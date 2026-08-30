using Application.Wallet.Features.Commands.ForceFreezeFromFraudAlert;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.ForceFreezeFromFraudAlert;

public sealed class ForceFreezeFromFraudAlertHandlerTests
{
    private readonly IWalletFraudAlertRepository _alertRepository = Substitute.For<IWalletFraudAlertRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly ForceFreezeFromFraudAlertHandler _sut;

    public ForceFreezeFromFraudAlertHandlerTests()
    {
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FakeLockHandle("wallet", true));
        _sut = new ForceFreezeFromFraudAlertHandler(
            _alertRepository, _walletRepository, _unitOfWork, _distributedLock, _auditService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenAlertNotFound_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _alertRepository.GetByIdAsync(Arg.Any<WalletFraudAlertId>(), Arg.Any<CancellationToken>())
            .Returns((WalletFraudAlert?)null);

        var result = await _sut.Handle(new ForceFreezeFromFraudAlertCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenAlertNotOpen_ReturnsConflict()
    {
        var adminId = UserId.NewId();
        _currentUserService.UserId.Returns(adminId.Value);
        var alert = new WalletFraudAlertBuilder().Build();
        alert.Dismiss(adminId, "test");
        _alertRepository.GetByIdAsync(Arg.Any<WalletFraudAlertId>(), Arg.Any<CancellationToken>()).Returns(alert);

        var result = await _sut.Handle(new ForceFreezeFromFraudAlertCommand(alert.Id.Value, null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenLockNotAcquired_ReturnsConflict()
    {
        var adminId = UserId.NewId();
        _currentUserService.UserId.Returns(adminId.Value);
        var alert = new WalletFraudAlertBuilder().Build();
        _alertRepository.GetByIdAsync(Arg.Any<WalletFraudAlertId>(), Arg.Any<CancellationToken>()).Returns(alert);
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((ILockHandle?)null);

        var result = await _sut.Handle(new ForceFreezeFromFraudAlertCommand(alert.Id.Value, null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenWalletNotFound_ReturnsNotFound()
    {
        var adminId = UserId.NewId();
        _currentUserService.UserId.Returns(adminId.Value);
        var alert = new WalletFraudAlertBuilder().Build();
        _alertRepository.GetByIdAsync(Arg.Any<WalletFraudAlertId>(), Arg.Any<CancellationToken>()).Returns(alert);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(new ForceFreezeFromFraudAlertCommand(alert.Id.Value, null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenValidAndWalletActive_FreezesWalletAndMarksReviewed()
    {
        var adminId = UserId.NewId();
        var ownerId = UserId.NewId();
        _currentUserService.UserId.Returns(adminId.Value);
        var alert = new WalletFraudAlertBuilder().WithUserId(ownerId).WithRuleName("HighVelocity").Build();
        var wallet = new WalletBuilder().WithOwnerId(ownerId).Build();
        _alertRepository.GetByIdAsync(Arg.Any<WalletFraudAlertId>(), Arg.Any<CancellationToken>()).Returns(alert);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(new ForceFreezeFromFraudAlertCommand(alert.Id.Value, "check needed"), CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.IsActive.ShouldBeFalse();
        wallet.FrozenBy.ShouldBe(adminId);
        alert.Status.ShouldBe(FraudAlertStatus.Reviewed);
        _walletRepository.Received(1).Update(wallet);
        _alertRepository.Received(1).Update(alert);
        await _auditService.Received(1).LogSystemEventAsync(
            "WalletForceFrozenFromFraudAlert", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWalletAlreadyInactive_DoesNotFreezeButMarksReviewed()
    {
        var adminId = UserId.NewId();
        var ownerId = UserId.NewId();
        _currentUserService.UserId.Returns(adminId.Value);
        var alert = new WalletFraudAlertBuilder().WithUserId(ownerId).Build();
        var wallet = new WalletBuilder().WithOwnerId(ownerId).Build();
        wallet.Freeze("prior freeze", UserId.NewId());

        _alertRepository.GetByIdAsync(Arg.Any<WalletFraudAlertId>(), Arg.Any<CancellationToken>()).Returns(alert);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(new ForceFreezeFromFraudAlertCommand(alert.Id.Value, null), CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.IsActive.ShouldBeFalse();
        alert.Status.ShouldBe(FraudAlertStatus.Reviewed);
    }

    [Fact]
    public async Task Handle_WhenConcurrencyExceptionThrown_ReturnsConflict()
    {
        var adminId = UserId.NewId();
        var ownerId = UserId.NewId();
        _currentUserService.UserId.Returns(adminId.Value);
        var alert = new WalletFraudAlertBuilder().WithUserId(ownerId).Build();
        var wallet = new WalletBuilder().WithOwnerId(ownerId).Build();

        _alertRepository.GetByIdAsync(Arg.Any<WalletFraudAlertId>(), Arg.Any<CancellationToken>()).Returns(alert);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new ConcurrencyException());

        var result = await _sut.Handle(new ForceFreezeFromFraudAlertCommand(alert.Id.Value, null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
    }
}
