using Application.Wallet.Features.Commands.ApproveWithdrawal;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Application.Wallet.Features.Commands.ApproveWithdrawal;

public sealed class ApproveWithdrawalHandlerTests : HandlerTestBase
{
    private readonly IWalletWithdrawalRepository _withdrawalRepository = Substitute.For<IWalletWithdrawalRepository>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly ApproveWithdrawalHandler _sut;

    public ApproveWithdrawalHandlerTests()
    {
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FakeLockHandle("withdrawal", true));

        _sut = new ApproveWithdrawalHandler(_withdrawalRepository, _distributedLock, AuditService, DateTimeProvider, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenWithdrawalNotFound_ReturnsNotFound()
    {
        var adminId = UserId.NewId();
        CurrentUserService.UserId.Returns(adminId.Value);
        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns((WalletWithdrawalRequest?)null);

        var result = await _sut.Handle(new ApproveWithdrawalCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenWithdrawalPending_ApprovesAndReturnsSuccess()
    {
        var adminId = UserId.NewId();
        CurrentUserService.UserId.Returns(adminId.Value);
        var withdrawal = new WalletWithdrawalRequestBuilder().WithAmount(200_000m).Build();
        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);

        var result = await _sut.Handle(new ApproveWithdrawalCommand(withdrawal.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        withdrawal.Status.ShouldBe(WalletWithdrawalStatus.Approved);
        withdrawal.ProcessedBy.ShouldBe(adminId);
        _withdrawalRepository.Received(1).Update(withdrawal);
        await UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await AuditService.Received(1).LogSystemEventAsync(
            "WithdrawalApproved",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWithdrawalAlreadyProcessed_ReturnsFailure()
    {
        var adminId = UserId.NewId();
        CurrentUserService.UserId.Returns(adminId.Value);
        var withdrawal = new WalletWithdrawalRequestBuilder().Build();
        withdrawal.Approve(adminId, DateTime.UtcNow);
        _withdrawalRepository.GetByIdForUpdateAsync(Arg.Any<WalletWithdrawalRequestId>(), Arg.Any<CancellationToken>())
            .Returns(withdrawal);

        var result = await _sut.Handle(new ApproveWithdrawalCommand(withdrawal.Id.Value), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}
