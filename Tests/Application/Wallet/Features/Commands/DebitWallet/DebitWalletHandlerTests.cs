using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Wallet.Features.Commands.DebitWallet;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.DebitWallet;

public class DebitWalletHandlerTests
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>(); private readonly ILockHandle _lockHandle = Substitute.For<ILockHandle>(); private readonly DebitWalletHandler _sut;

    public DebitWalletHandlerTests()
    {
        _lockHandle.IsAcquired.Returns(true);
        _lockHandle.ReleaseAsync().Returns(Task.CompletedTask);
        _distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_lockHandle);

        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());

        _sut = new DebitWalletHandler(
            _walletRepository,
            _unitOfWork,
            _auditService,
            _currentUserService,
            _distributedLock);
    }

    private static DebitWalletCommand ValidCommand(
        Guid? userId = null,
        decimal amount = 10_000m,
        string? idempotencyKey = null,
        string? referenceId = null) =>
        new(
            userId ?? Guid.NewGuid(),
            amount,
            WalletTransactionType.Debit,
            WalletReferenceType.System,
            idempotencyKey ?? "idem-" + Guid.NewGuid().ToString("N"),
            null,
            "debit description",
            referenceId);

    private static Wallets FundedWallet(Guid ownerId, decimal balance)
    {
        var wallet = new WalletBuilder().WithOwnerId(UserId.From(ownerId)).Build();
        if (balance > 0)
            wallet.Credit(Money.Create(balance), "initial-fund", "seed-ref");
        return wallet;
    }

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
    public async Task Handle_WhenIdempotencyKeyAlreadyProcessed_ReturnsSuccessWithoutLoadingWallet()
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
        _walletRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenWalletNotFound_ReturnsNotFound()
    {
        var command = ValidCommand();
        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _walletRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WithInsufficientBalance_ReturnsFailureAndDoesNotSaveChanges()
    {
        var command = ValidCommand(amount: 500_000m);
        var wallet = FundedWallet(command.UserId, balance: 100_000m);

        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        wallet.Balance.Amount.ShouldBe(100_000m);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WithSufficientBalance_DebitsWalletAndSavesChanges()
    {
        var command = ValidCommand(amount: 30_000m, referenceId: "external-ref");
        var wallet = FundedWallet(command.UserId, balance: 100_000m);

        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.Balance.Amount.ShouldBe(70_000m);
        _walletRepository.Received(1).Update(wallet);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSaveChangesThrowsConcurrencyException_ReturnsConflictAndAudits()
    {
        var command = ValidCommand(amount: 20_000m);
        var wallet = FundedWallet(command.UserId, balance: 100_000m);

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
            "WalletDebitConcurrencyConflict",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWalletIsInactive_ReturnsFailureFromDomainException()
    {
        var command = ValidCommand(amount: 10_000m);
        var wallet = FundedWallet(command.UserId, balance: 100_000m);
        wallet.Freeze("compliance-hold", UserId.NewId());

        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        wallet.Balance.Amount.ShouldBe(100_000m);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WithoutExplicitReferenceId_UsesCurrentUserIdAsReferenceAndSucceeds()
    {
        var callerId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)callerId);
        var command = ValidCommand(amount: 5_000m, referenceId: null);
        var wallet = FundedWallet(command.UserId, balance: 100_000m);

        _walletRepository
            .HasIdempotencyKeyAsync(Arg.Any<UserId>(), command.IdempotencyKey, Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.Balance.Amount.ShouldBe(95_000m);
        _walletRepository.Received(1).Update(wallet);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
