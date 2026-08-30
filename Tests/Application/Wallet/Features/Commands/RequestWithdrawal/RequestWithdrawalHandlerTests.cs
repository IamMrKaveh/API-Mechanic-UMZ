using Application.Wallet.Features.Commands.RequestWithdrawal;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.RequestWithdrawal;

public sealed class RequestWithdrawalHandlerTests
{
    private const string ValidIban = "IR580540105180021273113007";

    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IWalletWithdrawalRepository _withdrawalRepository = Substitute.For<IWalletWithdrawalRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly RequestWithdrawalHandler _sut;

    public RequestWithdrawalHandlerTests()
    {
        _sut = new RequestWithdrawalHandler(
            _walletRepository, _withdrawalRepository, _unitOfWork, _auditService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenIbanInvalid_ReturnsValidation()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());

        var result = await _sut.Handle(
            new RequestWithdrawalCommand(200_000m, "invalid-iban", "Ali Rezaei", null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WhenAccountHolderTooShort_ReturnsValidation()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());

        var result = await _sut.Handle(
            new RequestWithdrawalCommand(200_000m, ValidIban, "AB", null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WhenTooManyPendingWithdrawals_ReturnsConflict()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        _withdrawalRepository
            .CountByUserAndStatusAsync(userId, WalletWithdrawalStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(5);

        var result = await _sut.Handle(
            new RequestWithdrawalCommand(200_000m, ValidIban, "Ali Rezaei", null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenWalletDoesNotExist_CreatesWalletAndReturnsFailureForInactivity()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        _withdrawalRepository
            .CountByUserAndStatusAsync(userId, WalletWithdrawalStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(0);
        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(
            new RequestWithdrawalCommand(200_000m, ValidIban, "Ali Rezaei", null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _walletRepository.Received(1).AddAsync(Arg.Any<Wallets>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWalletInactive_ReturnsFailure()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        wallet.Freeze("suspicious", UserId.NewId());
        _withdrawalRepository
            .CountByUserAndStatusAsync(userId, WalletWithdrawalStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(0);
        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(
            new RequestWithdrawalCommand(200_000m, ValidIban, "Ali Rezaei", null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenInsufficientBalance_ReturnsValidation()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(50_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        _withdrawalRepository
            .CountByUserAndStatusAsync(userId, WalletWithdrawalStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(0);
        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(
            new RequestWithdrawalCommand(200_000m, ValidIban, "Ali Rezaei", null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesReservationAndWithdrawalAndReturnsSuccess()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        _withdrawalRepository
            .CountByUserAndStatusAsync(userId, WalletWithdrawalStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(0);
        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(
            new RequestWithdrawalCommand(200_000m, ValidIban, "Ali Rezaei", "for salary"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldNotBe(Guid.Empty);
        wallet.ActiveReservations.Count.ShouldBe(1);
        wallet.AvailableBalance.Amount.ShouldBe(300_000m);
        await _withdrawalRepository.Received(1).AddAsync(Arg.Any<WalletWithdrawalRequest>(), Arg.Any<CancellationToken>());
        _walletRepository.Received(1).Update(wallet);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenConcurrencyExceptionThrown_ReturnsConflict()
    {
        var userId = UserId.NewId();
        _currentUserService.UserId.Returns(userId.Value);
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        _withdrawalRepository
            .CountByUserAndStatusAsync(userId, WalletWithdrawalStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(0);
        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new ConcurrencyException());

        var result = await _sut.Handle(
            new RequestWithdrawalCommand(200_000m, ValidIban, "Ali Rezaei", null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
        await _auditService.Received().LogSystemEventAsync(
            "WithdrawalRequestConcurrencyConflict", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
