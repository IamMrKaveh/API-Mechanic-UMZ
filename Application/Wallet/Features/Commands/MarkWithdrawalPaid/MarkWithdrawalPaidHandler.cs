using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.MarkWithdrawalPaid;

public sealed class MarkWithdrawalPaidHandler(
    IWalletWithdrawalRepository withdrawalRepository,
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IDistributedLock distributedLock,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<MarkWithdrawalPaidCommand, Unit>
{
    private static readonly TimeSpan WalletLockExpiry = TimeSpan.FromSeconds(10);

    public async Task<ServiceResult<Unit>> Handle(
        MarkWithdrawalPaidCommand request,
        CancellationToken ct)
    {
        var withdrawalId = WalletWithdrawalRequestId.From(request.WithdrawalId);
        var adminId = UserId.From(currentUserService.UserId.Value);

        var withdrawalForLookup = await withdrawalRepository.GetByIdForUpdateAsync(withdrawalId, ct);
        if (withdrawalForLookup is null)
            return ServiceResult<Unit>.NotFound("درخواست برداشت یافت نشد.");

        var userId = withdrawalForLookup.UserId;

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"wallet:{userId.Value:N}",
            WalletLockExpiry,
            ct);

        if (lockHandle is null || !lockHandle.IsAcquired)
            return ServiceResult<Unit>.Conflict("عملیات دیگری روی کیف پول در حال انجام است. لطفاً مجدداً تلاش کنید.");

        try
        {
            var withdrawal = await withdrawalRepository.GetByIdForUpdateAsync(withdrawalId, ct);
            if (withdrawal is null)
                return ServiceResult<Unit>.NotFound("درخواست برداشت یافت نشد.");

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(withdrawal.UserId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول کاربر یافت نشد.");

            wallet.ReleaseReservation(withdrawal.ReservationId);
            wallet.Debit(
                withdrawal.Amount,
                $"برداشت به شماره پیگیری {request.BankReferenceNumber}",
                withdrawal.Id.Value.ToString());

            withdrawal.MarkPaid(adminId, request.BankReferenceNumber);

            walletRepository.Update(wallet);
            withdrawalRepository.Update(withdrawal);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSystemEventAsync(
                "WithdrawalMarkedPaid",
                $"درخواست برداشت {withdrawal.Id.Value} توسط ادمین {adminId.Value} پرداخت‌شده علامت‌گذاری شد. شماره پیگیری بانکی: {request.BankReferenceNumber}.",
                ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (InsufficientWalletBalanceException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WithdrawalMarkPaidConcurrencyConflict",
                $"تعارض همزمانی در علامت‌گذاری پرداخت درخواست برداشت {request.WithdrawalId}.",
                ct);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}
