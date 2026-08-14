using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Wallet.Features.Commands.CreditWallet;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.CreditWallet;

public class CreditWalletHandlerTests
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly ILockHandle _lockHandle = Substitute.For<ILockHandle>(); private readonly CreditWalletHandler _sut;

    public CreditWalletHandlerTests()
    {
        _lockHandle.IsAcquired.Returns(true);
        _lockHandle.ReleaseAsync().Returns(Task.CompletedTask);
        _distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_lockHandle);

        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUserService.IsAdmin.Returns(false);

        _sut = new CreditWalletHandler(
            _walletRepository,
            _unitOfWork,
            _distributedLock,
            _auditService,
            _currentUserService);
    }

    private static CreditWalletCommand ValidCommand(
        Guid? userId = null,
        decimal amount = 100_000m,
        string? idempotencyKey = null,
        string? referenceId = null,
        AdminWalletAdjustmentType? adjustmentType = null) =>
        new(
            userId ?? Guid.NewGuid(),
            amount,
            WalletTransactionType.Credit,
            WalletReferenceType.System,
            referenceId ?? "ref-" + Guid.NewGuid().ToString("N"),
            idempotencyKey ?? "idem-" + Guid.NewGuid().ToString("N"),
            null,
            "credit description",
            adjustmentType);

    [Fact]
    public async Task Handle_WhenDistributedLockNotAcquired_ReturnsConflictAndDoesNotTouchRepository()
    {
        _distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((ILockHandle?)null);

        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _walletRepository.DidNotReceiveWithAnyArgs()
            .HasIdempotencyKeyAsync(default!, default!, default);
        await _walletRepository.DidNotReceiveWithAnyArgs()
            .GetByUserIdForUpdateAsync(default!, default);
        _walletRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenLockHandleReportsNotAcquired_ReturnsConflict()
    {
        var notAcquired = Substitute.For<ILockHandle>();
        notAcquired.IsAcquired.Returns(false);
        notAcquired.ReleaseAsync().Returns(Task.CompletedTask);
        _distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(notAcquired);

        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _walletRepository.DidNotReceiveWithAnyArgs()
            .HasIdempotencyKeyAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyProcessed_ReturnsSuccessAndSkipsPersistence()
    {
        var command = ValidCommand();
        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(Unit.Value);
        await _walletRepository.DidNotReceiveWithAnyArgs()
            .GetByUserIdForUpdateAsync(default!, default);
        await _walletRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        _walletRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenWalletDoesNotExist_CreatesWalletAndCreditsIt()
    {
        var command = ValidCommand(amount: 50_000m);
        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        Wallets? addedWallet = null;
        _walletRepository
            .AddAsync(Arg.Do<Wallets>(w => addedWallet = w), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(Unit.Value);
        addedWallet.ShouldNotBeNull();
        addedWallet!.OwnerId.Value.ShouldBe(command.UserId);
        addedWallet.Balance.Amount.ShouldBe(50_000m);
        _walletRepository.Received(1).Update(addedWallet);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWalletExists_CreditsExistingWalletAndSavesChanges()
    {
        var command = ValidCommand(amount: 25_000m);
        var wallet = new WalletBuilder().WithOwnerId(UserId.From(command.UserId)).Build();

        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.Balance.Amount.ShouldBe(25_000m);
        _walletRepository.Received(1).Update(wallet);
        await _walletRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWalletIsFrozenAndCallerIsAdmin_AutoUnfreezesAndCreditsWallet()
    {
        var command = ValidCommand(amount: 10_000m);
        var wallet = new WalletBuilder().WithOwnerId(UserId.From(command.UserId)).Build();
        wallet.Freeze("initial-freeze", UserId.NewId());

        _currentUserService.IsAdmin.Returns(true);
        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.IsActive.ShouldBeTrue();
        wallet.Balance.Amount.ShouldBe(10_000m);
        _walletRepository.Received(1).Update(wallet);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogSystemEventAsync(
            "WalletAutoUnfrozenOnAdminCredit",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWalletIsFrozenAndCallerIsNotAdmin_DoesNotAutoUnfreezeAndDomainRejects()
    {
        var command = ValidCommand(amount: 10_000m);
        var wallet = new WalletBuilder().WithOwnerId(UserId.From(command.UserId)).Build();
        wallet.Freeze("initial-freeze", UserId.NewId());

        _currentUserService.IsAdmin.Returns(false);
        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.IsActive.ShouldBeFalse();
        wallet.Balance.Amount.ShouldBe(10_000m);
        await _auditService.DidNotReceive().LogSystemEventAsync(
            "WalletAutoUnfrozenOnAdminCredit",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSaveChangesThrowsConcurrencyException_ReturnsConflictAndAudits()
    {
        var command = ValidCommand();
        var wallet = new WalletBuilder().WithOwnerId(UserId.From(command.UserId)).Build();

        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyException());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _auditService.Received(1).LogSystemEventAsync(
            "WalletCreditConcurrencyConflict",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDomainExceptionThrown_ReturnsFailureWithExceptionMessage()
    {
        var command = ValidCommand();
        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DomainException("domain-error-message"));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.Error.Message.ShouldBe("domain-error-message");
    }

    [Fact]
    public async Task Handle_WithAdjustmentType_IncludesAdjustmentTypeMarkerInPersistedDescription()
    {
        var command = ValidCommand(
            amount: 5_000m,
            adjustmentType: AdminWalletAdjustmentType.Compensation);
        var wallet = new WalletBuilder().WithOwnerId(UserId.From(command.UserId)).Build();

        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.Balance.Amount.ShouldBe(5_000m);
        _walletRepository.Received(1).Update(wallet);
    }

    [Fact]
    public async Task Handle_ChecksIdempotencyBeforeLoadingWallet()
    {
        var command = ValidCommand();
        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.Handle(command, CancellationToken.None);

        await _walletRepository.Received(1)
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>());
        await _walletRepository.DidNotReceiveWithAnyArgs()
            .GetByUserIdForUpdateAsync(default!, default);
    }
}
