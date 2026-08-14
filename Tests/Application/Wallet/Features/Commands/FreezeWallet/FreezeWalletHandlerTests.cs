using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Wallet.Features.Commands.FreezeWallet;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.FreezeWallet;

public class FreezeWalletHandlerTests
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly ILockHandle _lockHandle = Substitute.For<ILockHandle>(); private readonly FreezeWalletHandler _sut;

    public FreezeWalletHandlerTests()
    {
        _lockHandle.IsAcquired.Returns(true);
        _lockHandle.ReleaseAsync().Returns(Task.CompletedTask);
        _distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_lockHandle);

        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());

        _sut = new FreezeWalletHandler(
            _walletRepository,
            _unitOfWork,
            _distributedLock,
            _auditService,
            _currentUserService);
    }

    private static FreezeWalletCommand ValidCommand(Guid? userId = null, string reason = "compliance-hold") =>
        new(userId ?? Guid.NewGuid(), reason);

    [Fact]
    public async Task Handle_WhenDistributedLockNotAcquired_ReturnsConflict()
    {
        _distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((ILockHandle?)null);

        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _walletRepository.DidNotReceiveWithAnyArgs()
            .GetByUserIdForUpdateAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenWalletNotFound_ReturnsNotFound()
    {
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _walletRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenWalletActive_FreezesWalletAndSavesChangesAndAudits()
    {
        var command = ValidCommand(reason: "suspicious-activity");
        var wallet = new WalletBuilder().WithOwnerId(UserId.From(command.UserId)).Build();

        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.IsActive.ShouldBeFalse();
        wallet.FreezeReason.ShouldBe("suspicious-activity");
        wallet.FrozenAt.ShouldNotBeNull();
        wallet.FrozenBy.ShouldNotBeNull();
        _walletRepository.Received(1).Update(wallet);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogSystemEventAsync(
            "WalletFrozen",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWalletAlreadyFrozen_ReturnsSuccessWithoutStateChange()
    {
        var command = ValidCommand(reason: "compliance-hold");
        var wallet = new WalletBuilder().WithOwnerId(UserId.From(command.UserId)).Build();
        var initialAdmin = UserId.NewId();
        wallet.Freeze("original-freeze", initialAdmin);

        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.IsActive.ShouldBeFalse();
        wallet.FreezeReason.ShouldBe("original-freeze");
        wallet.FrozenBy!.Value.ShouldBe(initialAdmin.Value);
    }

    [Fact]
    public async Task Handle_WhenSaveChangesThrowsConcurrencyException_ReturnsConflictAndAudits()
    {
        var command = ValidCommand();
        var wallet = new WalletBuilder().WithOwnerId(UserId.From(command.UserId)).Build();

        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyException());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _auditService.Received(1).LogSystemEventAsync(
            "WalletFreezeConcurrencyConflict",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDomainExceptionThrown_ReturnsFailureWithMessage()
    {
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DomainException("wallet-domain-error"));

        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.Error.Message.ShouldBe("wallet-domain-error");
    }
}
