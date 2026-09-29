using Application.Wallet.Features.Commands.RequestWalletDebit;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using SharedKernel.Abstractions.Interfaces;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.RequestWalletDebit;

public sealed class RequestWalletDebitHandlerTests : HandlerTestBase
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly RequestWalletDebitHandler _sut;

    public RequestWalletDebitHandlerTests()
    {
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FakeLockHandle("wallet", true));

        _sut = new RequestWalletDebitHandler(_walletRepository, _distributedLock, DateTimeProvider, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenLockNotAcquired_ReturnsConflict()
    {
        CurrentUserService.UserId.Returns(Guid.NewGuid());
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((ILockHandle?)null);

        var result = await _sut.Handle(
            new RequestWalletDebitCommand(Guid.NewGuid(), 100_000m, "reason", null, "key-1"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenWalletNotFound_ReturnsNotFound()
    {
        CurrentUserService.UserId.Returns(Guid.NewGuid());
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(
            new RequestWalletDebitCommand(Guid.NewGuid(), 100_000m, "reason", null, "key-1"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesDebitRequestAndReturnsRequestId()
    {
        CurrentUserService.UserId.Returns(Guid.NewGuid());
        var userId = UserId.NewId();
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), DateTime.UtcNow, Guid.NewGuid().ToString("N"));
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(
            new RequestWalletDebitCommand(userId.Value, 100_000m, "penalty", "desc", "idem-1"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldNotBe(Guid.Empty);
        wallet.DebitRequests.Count.ShouldBe(1);
        _walletRepository.Received(1).Update(wallet);
        await UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenInsufficientBalance_ReturnsFailure()
    {
        CurrentUserService.UserId.Returns(Guid.NewGuid());
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
        CurrentUserService.UserId.Returns(Guid.NewGuid());
        var userId = UserId.NewId();
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), DateTime.UtcNow, Guid.NewGuid().ToString("N"));
        wallet.Freeze("suspicious", UserId.NewId(), DateTime.UtcNow);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(
            new RequestWalletDebitCommand(userId.Value, 100_000m, "penalty", null, "idem-1"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}
