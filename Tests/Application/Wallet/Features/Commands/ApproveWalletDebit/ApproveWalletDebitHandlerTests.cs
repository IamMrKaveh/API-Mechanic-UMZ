using Application.Wallet.Features.Commands.ApproveWalletDebit;
using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.ApproveWalletDebit;

public sealed class ApproveWalletDebitHandlerTests
{
    private readonly IWalletDebitRequestRepository _debitRequestRepository = Substitute.For<IWalletDebitRequestRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly ApproveWalletDebitHandler _sut;

    public ApproveWalletDebitHandlerTests()
    {
        _sut = new ApproveWalletDebitHandler(
            _debitRequestRepository,
            _walletRepository,
            _unitOfWork,
            _distributedLock,
            _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenDebitRequestNotFound_ReturnsNotFound()
    {
        var ownerId = UserId.NewId();
        _currentUserService.UserId.Returns(ownerId.Value);
        var command = new ApproveWalletDebitCommand(Guid.NewGuid());
        _debitRequestRepository.GetByIdAsync(Arg.Any<WalletDebitRequestId>(), Arg.Any<CancellationToken>())
            .Returns((WalletDebitRequest?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsNotOwner_ReturnsForbidden()
    {
        var ownerId = UserId.NewId();
        var otherUserId = UserId.NewId();
        _currentUserService.UserId.Returns(otherUserId.Value);

        var (_, request) = new WalletDebitRequestBuilder().WithOwner(ownerId).Build();
        _debitRequestRepository.GetByIdAsync(Arg.Any<WalletDebitRequestId>(), Arg.Any<CancellationToken>()).Returns(request);

        var result = await _sut.Handle(new ApproveWalletDebitCommand(request.Id.Value), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenLockNotAcquired_ReturnsConflict()
    {
        var ownerId = UserId.NewId();
        _currentUserService.UserId.Returns(ownerId.Value);

        var (_, request) = new WalletDebitRequestBuilder().WithOwner(ownerId).Build();
        _debitRequestRepository.GetByIdAsync(Arg.Any<WalletDebitRequestId>(), Arg.Any<CancellationToken>()).Returns(request);
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((ILockHandle?)null);

        var result = await _sut.Handle(new ApproveWalletDebitCommand(request.Id.Value), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenLockHandleIsNotAcquired_ReturnsConflict()
    {
        var ownerId = UserId.NewId();
        _currentUserService.UserId.Returns(ownerId.Value);

        var (_, request) = new WalletDebitRequestBuilder().WithOwner(ownerId).Build();
        _debitRequestRepository.GetByIdAsync(Arg.Any<WalletDebitRequestId>(), Arg.Any<CancellationToken>()).Returns(request);
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FakeLockHandle("wallet", isAcquired: false));

        var result = await _sut.Handle(new ApproveWalletDebitCommand(request.Id.Value), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenWalletNotFound_ReturnsNotFound()
    {
        var ownerId = UserId.NewId();
        _currentUserService.UserId.Returns(ownerId.Value);
        var (_, request) = new WalletDebitRequestBuilder().WithOwner(ownerId).Build();
        _debitRequestRepository.GetByIdAsync(Arg.Any<WalletDebitRequestId>(), Arg.Any<CancellationToken>()).Returns(request);
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FakeLockHandle("wallet", true));
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(new ApproveWalletDebitCommand(request.Id.Value), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenValid_ApprovesRequestAndReturnsSuccess()
    {
        var ownerId = UserId.NewId();
        _currentUserService.UserId.Returns(ownerId.Value);
        var (wallet, request) = new WalletDebitRequestBuilder().WithOwner(ownerId).WithInitialBalance(500_000m).WithAmount(100_000m).Build();
        _debitRequestRepository.GetByIdAsync(Arg.Any<WalletDebitRequestId>(), Arg.Any<CancellationToken>()).Returns(request);
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FakeLockHandle("wallet", true));
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(new ApproveWalletDebitCommand(request.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.Balance.Amount.ShouldBe(400_000m);
        _walletRepository.Received(1).Update(wallet);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRequestAlreadyApproved_ReturnsFailure()
    {
        var ownerId = UserId.NewId();
        _currentUserService.UserId.Returns(ownerId.Value);
        var (wallet, request) = new WalletDebitRequestBuilder().WithOwner(ownerId).WithInitialBalance(500_000m).WithAmount(100_000m).Build();
        wallet.ApproveDebitRequest(request.Id, ownerId);

        _debitRequestRepository.GetByIdAsync(Arg.Any<WalletDebitRequestId>(), Arg.Any<CancellationToken>()).Returns(request);
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FakeLockHandle("wallet", true));
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(new ApproveWalletDebitCommand(request.Id.Value), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}
