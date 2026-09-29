using Application.Wallet.Features.Commands.RejectWalletDebit;
using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.RejectWalletDebit;

public sealed class RejectWalletDebitHandlerTests : HandlerTestBase
{
    private readonly IWalletDebitRequestRepository _debitRequestRepository = Substitute.For<IWalletDebitRequestRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly RejectWalletDebitHandler _sut;

    public RejectWalletDebitHandlerTests()
    {
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FakeLockHandle("wallet", true));

        _sut = new RejectWalletDebitHandler(
            _debitRequestRepository, _walletRepository, _distributedLock, DateTimeProvider, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenDebitRequestNotFound_ReturnsNotFound()
    {
        CurrentUserService.UserId.Returns(Guid.NewGuid());
        _debitRequestRepository.GetByIdAsync(Arg.Any<WalletDebitRequestId>(), Arg.Any<CancellationToken>())
            .Returns((WalletDebitRequest?)null);

        var result = await _sut.Handle(new RejectWalletDebitCommand(Guid.NewGuid(), "reason"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwner_ReturnsForbidden()
    {
        var ownerId = UserId.NewId();
        var otherUser = UserId.NewId();
        CurrentUserService.UserId.Returns(otherUser.Value);
        var (_, request) = new WalletDebitRequestBuilder().WithOwner(ownerId).Build();
        _debitRequestRepository.GetByIdAsync(Arg.Any<WalletDebitRequestId>(), Arg.Any<CancellationToken>()).Returns(request);

        var result = await _sut.Handle(new RejectWalletDebitCommand(request.Id.Value, "no"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenLockNotAcquired_ReturnsConflict()
    {
        var ownerId = UserId.NewId();
        CurrentUserService.UserId.Returns(ownerId.Value);
        var (_, request) = new WalletDebitRequestBuilder().WithOwner(ownerId).Build();
        _debitRequestRepository.GetByIdAsync(Arg.Any<WalletDebitRequestId>(), Arg.Any<CancellationToken>()).Returns(request);
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((ILockHandle?)null);

        var result = await _sut.Handle(new RejectWalletDebitCommand(request.Id.Value, "no"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenWalletNotFound_ReturnsNotFound()
    {
        var ownerId = UserId.NewId();
        CurrentUserService.UserId.Returns(ownerId.Value);
        var (_, request) = new WalletDebitRequestBuilder().WithOwner(ownerId).Build();
        _debitRequestRepository.GetByIdAsync(Arg.Any<WalletDebitRequestId>(), Arg.Any<CancellationToken>()).Returns(request);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(new RejectWalletDebitCommand(request.Id.Value, "no"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenValid_RejectsRequestAndReleasesReservation()
    {
        var ownerId = UserId.NewId();
        CurrentUserService.UserId.Returns(ownerId.Value);
        var (wallet, request) = new WalletDebitRequestBuilder()
            .WithOwner(ownerId).WithInitialBalance(500_000m).WithAmount(100_000m).Build();
        _debitRequestRepository.GetByIdAsync(Arg.Any<WalletDebitRequestId>(), Arg.Any<CancellationToken>()).Returns(request);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(new RejectWalletDebitCommand(request.Id.Value, "user says no"), CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.AvailableBalance.Amount.ShouldBe(500_000m);
        _walletRepository.Received(1).Update(wallet);
        await UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRequestAlreadyProcessed_ReturnsFailure()
    {
        var ownerId = UserId.NewId();
        CurrentUserService.UserId.Returns(ownerId.Value);
        var (wallet, request) = new WalletDebitRequestBuilder()
            .WithOwner(ownerId).WithInitialBalance(500_000m).WithAmount(100_000m).Build();
        wallet.RejectDebitRequest(request.Id, ownerId, "first rejection", DateTime.UtcNow);

        _debitRequestRepository.GetByIdAsync(Arg.Any<WalletDebitRequestId>(), Arg.Any<CancellationToken>()).Returns(request);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(new RejectWalletDebitCommand(request.Id.Value, "again"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}
