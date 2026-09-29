using Application.Wallet.Features.Commands.RequestWithdrawal;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using SharedKernel.Abstractions.Interfaces;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.RequestWithdrawal;

public sealed class RequestWithdrawalHandlerTests : HandlerTestBase
{
    private const string ValidIban = "IR580540105180021273113007";

    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IWalletWithdrawalRepository _withdrawalRepository = Substitute.For<IWalletWithdrawalRepository>();
    private readonly RequestWithdrawalHandler _sut;

    public RequestWithdrawalHandlerTests()
    {

        _sut = new RequestWithdrawalHandler(
            _walletRepository, _withdrawalRepository, AuditService, DateTimeProvider, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenIbanInvalid_ReturnsValidation()
    {
        CurrentUserService.UserId.Returns(Guid.NewGuid());

        var result = await _sut.Handle(
            new RequestWithdrawalCommand(200_000m, "invalid-iban", "Ali Rezaei", null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WhenAccountHolderTooShort_ReturnsValidation()
    {
        CurrentUserService.UserId.Returns(Guid.NewGuid());

        var result = await _sut.Handle(
            new RequestWithdrawalCommand(200_000m, ValidIban, "AB", null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WhenTooManyPendingWithdrawals_ReturnsConflict()
    {
        var userId = UserId.NewId();
        CurrentUserService.UserId.Returns(userId.Value);
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
        CurrentUserService.UserId.Returns(userId.Value);
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
        CurrentUserService.UserId.Returns(userId.Value);
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), DateTime.UtcNow, Guid.NewGuid().ToString("N"));
        wallet.Freeze("suspicious", UserId.NewId(), DateTime.UtcNow);
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
        CurrentUserService.UserId.Returns(userId.Value);
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(50_000m), "seed", Guid.NewGuid().ToString(), DateTime.UtcNow, Guid.NewGuid().ToString("N"));
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
        CurrentUserService.UserId.Returns(userId.Value);
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), DateTime.UtcNow, Guid.NewGuid().ToString("N"));
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
    }
}
