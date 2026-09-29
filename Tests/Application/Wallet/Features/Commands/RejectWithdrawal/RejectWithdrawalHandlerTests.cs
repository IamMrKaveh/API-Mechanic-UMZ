using Application.Wallet.Features.Commands.RejectWithdrawal;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.RejectWithdrawal;

public sealed class RejectWithdrawalHandlerTests : HandlerTestBase
{
    private readonly IWalletWithdrawalRepository _withdrawalRepository = Substitute.For<IWalletWithdrawalRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly RejectWithdrawalHandler _sut;

    public RejectWithdrawalHandlerTests()
    {

        _sut = new RejectWithdrawalHandler(
            _withdrawalRepository, _walletRepository, AuditService, DateTimeProvider, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenWithdrawalNotFound_ReturnsNotFound()
    {
        CurrentUserService.UserId.Returns(Guid.NewGuid());
        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns((WalletWithdrawalRequest?)null);

        var result = await _sut.Handle(new RejectWithdrawalCommand(Guid.NewGuid(), "reason"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenWalletNotFound_ReturnsNotFound()
    {
        var adminId = UserId.NewId();
        CurrentUserService.UserId.Returns(adminId.Value);
        var withdrawal = new WalletWithdrawalRequestBuilder().Build();
        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(new RejectWithdrawalCommand(withdrawal.Id.Value, "reason"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenValid_ReleasesReservationAndRejectsWithdrawal()
    {
        var adminId = UserId.NewId();
        var userId = UserId.NewId();
        CurrentUserService.UserId.Returns(adminId.Value);

        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), DateTime.UtcNow, Guid.NewGuid().ToString("N"));
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Money.Create(200_000m), "withdrawal-request", DateTime.UtcNow);

        var withdrawal = new WalletWithdrawalRequestBuilder()
            .WithUserId(userId).WithAmount(200_000m).WithReservationId(reservationId).Build();

        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);
        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(new RejectWithdrawalCommand(withdrawal.Id.Value, "invalid iban"), CancellationToken.None);

        result.ShouldBeSuccess();
        withdrawal.Status.ShouldBe(WalletWithdrawalStatus.Rejected);
        withdrawal.RejectionReason.ShouldBe("invalid iban");
        wallet.AvailableBalance.Amount.ShouldBe(500_000m);
        _walletRepository.Received(1).Update(wallet);
        _withdrawalRepository.Received(1).Update(withdrawal);
    }

    [Fact]
    public async Task Handle_WhenWithdrawalAlreadyRejected_ReturnsFailure()
    {
        var adminId = UserId.NewId();
        var userId = UserId.NewId();
        CurrentUserService.UserId.Returns(adminId.Value);

        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(500_000m), "seed", Guid.NewGuid().ToString(), DateTime.UtcNow, Guid.NewGuid().ToString("N"));
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Money.Create(200_000m), "withdrawal-request", DateTime.UtcNow);

        var withdrawal = new WalletWithdrawalRequestBuilder()
            .WithUserId(userId).WithAmount(200_000m).WithReservationId(reservationId).Build();
        withdrawal.Reject(adminId, "first reject", DateTime.UtcNow);

        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);
        _walletRepository.GetByUserIdForUpdateAsync(userId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(new RejectWithdrawalCommand(withdrawal.Id.Value, "again"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}
