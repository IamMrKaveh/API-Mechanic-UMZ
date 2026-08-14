using Application.Audit.Contracts;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Wallet.Features.Commands.UnfreezeWallet;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.UnfreezeWallet;

public class UnfreezeWalletHandlerTests
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly UnfreezeWalletHandler _sut;

    public UnfreezeWalletHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());

        _sut = new UnfreezeWalletHandler(
            _walletRepository,
            _unitOfWork,
            _auditService,
            _currentUserService);
    }

    private static UnfreezeWalletCommand ValidCommand(Guid? userId = null) =>
        new(userId ?? Guid.NewGuid());

    [Fact]
    public async Task Handle_WhenWalletDoesNotExist_CreatesEmptyWalletAndReturnsSuccessAndAudits()
    {
        var command = ValidCommand();
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        Wallets? addedWallet = null;
        _walletRepository
            .AddAsync(Arg.Do<Wallets>(w => addedWallet = w), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        addedWallet.ShouldNotBeNull();
        addedWallet!.OwnerId.Value.ShouldBe(command.UserId);
        addedWallet.IsActive.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogSystemEventAsync(
            "WalletAutoCreatedOnUnfreeze",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWalletExistsAndFrozen_UnfreezesWalletAndSavesChangesAndAudits()
    {
        var command = ValidCommand();
        var wallet = new WalletBuilder().WithOwnerId(UserId.From(command.UserId)).Build();
        wallet.Freeze("prior-freeze", UserId.NewId());

        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.IsActive.ShouldBeTrue();
        wallet.FreezeReason.ShouldBeNull();
        wallet.FrozenAt.ShouldBeNull();
        wallet.FrozenBy.ShouldBeNull();
        _walletRepository.Received(1).Update(wallet);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogSystemEventAsync(
            "WalletUnfrozen",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWalletExistsAndAlreadyActive_KeepsWalletActiveAndReturnsSuccess()
    {
        var command = ValidCommand();
        var wallet = new WalletBuilder().WithOwnerId(UserId.From(command.UserId)).Build();

        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        wallet.IsActive.ShouldBeTrue();
        _walletRepository.Received(1).Update(wallet);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSaveChangesThrowsConcurrencyException_ReturnsConflictAndAudits()
    {
        var command = ValidCommand();
        var wallet = new WalletBuilder().WithOwnerId(UserId.From(command.UserId)).Build();
        wallet.Freeze("prior", UserId.NewId());

        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyException());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _auditService.Received(1).LogSystemEventAsync(
            "WalletUnfreezeConcurrencyConflict",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDomainExceptionThrown_ReturnsFailureWithMessage()
    {
        _walletRepository
            .GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DomainException("unfreeze-domain-error"));

        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.Error.Message.ShouldBe("unfreeze-domain-error");
    }
}
