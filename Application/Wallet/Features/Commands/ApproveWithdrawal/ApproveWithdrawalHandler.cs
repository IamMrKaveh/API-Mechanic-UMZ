using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.ApproveWithdrawal;

public sealed class ApproveWithdrawalHandler(
    IWalletWithdrawalRepository withdrawalRepository,
    IUnitOfWork unitOfWork,
    IDistributedLock distributedLock,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<ApproveWithdrawalCommand, Unit>
{
    private static readonly TimeSpan WithdrawalLockExpiry = TimeSpan.FromSeconds(10);

    public async Task<ServiceResult<Unit>> Handle(
        ApproveWithdrawalCommand request,
        CancellationToken ct)
    {
        var withdrawalId = WalletWithdrawalRequestId.From(request.WithdrawalId);
        var adminId = UserId.From(currentUserService.UserId!.Value);

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"withdrawal:{withdrawalId.Value:N}",
            WithdrawalLockExpiry,
            ct);

        if (lockHandle is null || !lockHandle.IsAcquired)
            return ServiceResult<Unit>.Conflict("عملیات دیگری روی این درخواست برداشت در حال انجام است. لطفاً مجدداً تلاش کنید.");

        try
        {
            var withdrawal = await withdrawalRepository.GetByIdForUpdateAsync(withdrawalId, ct);
            if (withdrawal is null)
                return ServiceResult<Unit>.NotFound("درخواست برداشت یافت نشد.");

            withdrawal.Approve(adminId);
            withdrawalRepository.Update(withdrawal);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSystemEventAsync(
                "WithdrawalApproved",
                $"درخواست برداشت {withdrawal.Id.Value} توسط ادمین {adminId.Value} تأیید شد.",
                ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WithdrawalApproveConcurrencyConflict",
                $"تعارض همزمانی در تأیید درخواست برداشت {request.WithdrawalId}.",
                ct);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}
