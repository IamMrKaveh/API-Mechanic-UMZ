using Application.Wallet.Features.Commands.RequestWalletDebit;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.RequestWalletDebit;

public sealed class RequestWalletDebitHandlerTests
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly RequestWalletDebitHandler _sut;

    public RequestWalletDebitHandlerTests()
    {
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FakeLockHandle("wallet", true));
        _sut = new RequestWalletDebitHandler(_walletRepository, _unitOfWork, _distributedLock, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenLockNotAcquired_ReturnsConflict()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((ILockHandle?)null);

        var result = await _sut.Handle(
            new RequestWalletDebitCommand(Guid.NewGuid(), 100_000m, "reason", null, "key-1"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenWalletNotFound_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(
            new RequestWalletDebitCommand(Guid.NewGuid(), 100_000m, "reason", null, "key-1"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesDebitRequestAndReturnsRequestId()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        var userId = UserId.NewId();
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(
            new RequestWalletDebitCommand(userId.Value, 100_000m, "penalty", "desc", "idem-1"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldNotBe(Guid.Empty);
        wallet.DebitRequests.Count.ShouldBe(1);
        _walletRepository.Received(1).Update(wallet);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenInsufficientBalance_ReturnsFailure()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        var userId = UserId.NewId();
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(
            new RequestWalletDebitCommand(userId.Value, 100_000m, "penalty", null, "idem-1"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenWalletInactive_ReturnsFailure()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        var userId = UserId.NewId();
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        wallet.Freeze("suspicious", UserId.NewId());
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(
            new RequestWalletDebitCommand(userId.Value, 100_000m, "penalty", null, "idem-1"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenConcurrencyExceptionThrown_ReturnsConflict()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        var userId = UserId.NewId();
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new ConcurrencyException());

        var result = await _sut.Handle(
            new RequestWalletDebitCommand(userId.Value, 100_000m, "penalty", null, "idem-1"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
    }
}
