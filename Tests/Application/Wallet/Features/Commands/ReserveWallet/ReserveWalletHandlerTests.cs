using Application.Audit.Contracts;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Wallet.Features.Commands.ReserveWallet;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.ReserveWallet;

public class ReserveWalletHandlerTests
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ReserveWalletHandler _sut;

    public ReserveWalletHandlerTests()
    {
        _sut = new ReserveWalletHandler(
            _walletRepository,
            _unitOfWork,
            _auditService);
    }

    private static ReserveWalletCommand ValidCommand(
        Guid? userId = null,
        decimal amount = 10_000m,
        Guid? walletId = null) =>
        new(
            userId ?? Guid.NewGuid(),
            amount,
            walletId ?? Guid.NewGuid());

    private static Wallets FundedWallet(Guid ownerId, decimal balance)
    {
        var wallet = new WalletBuilder().WithOwnerId(UserId.From(ownerId)).Build();
        if (balance > 0)
            wallet.Credit(Money.Create(balance), "seed-fund", "seed-ref");
        return wallet;
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
    public async Task Handle_WithSufficientBalance_CreatesReservationAndSavesChanges()
    {
        var command = ValidCommand(amount: 20_000m);
        var wallet = FundedWallet(command.UserId, balance: 100_000m);

        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.ActiveReservations.Count.ShouldBe(1);
        wallet.ActiveReservations[0].Amount.Amount.ShouldBe(20_000m);
        wallet.Balance.Amount.ShouldBe(100_000m);
        wallet.AvailableBalance.Amount.ShouldBe(80_000m);
        _walletRepository.Received(1).Update(wallet);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInsufficientBalance_ReturnsFailureAndDoesNotPersist()
    {
        var command = ValidCommand(amount: 500_000m);
        var wallet = FundedWallet(command.UserId, balance: 100_000m);

        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        wallet.ActiveReservations.ShouldBeEmpty();
        _walletRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenWalletIsInactive_ReturnsFailureFromDomainException()
    {
        var command = ValidCommand(amount: 10_000m);
        var wallet = FundedWallet(command.UserId, balance: 100_000m);
        wallet.Freeze("hold", UserId.NewId());

        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        wallet.ActiveReservations.ShouldBeEmpty();
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenSaveChangesThrowsConcurrencyException_ReturnsConflictAndAudits()
    {
        var command = ValidCommand(amount: 10_000m);
        var wallet = FundedWallet(command.UserId, balance: 100_000m);

        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyException());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _auditService.Received(1).LogSystemEventAsync(
            "WalletReserveConcurrencyConflict",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
