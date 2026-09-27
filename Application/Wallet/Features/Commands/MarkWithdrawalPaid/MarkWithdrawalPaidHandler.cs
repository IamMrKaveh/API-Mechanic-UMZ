using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Wallet.Features.Commands.MarkWithdrawalPaid;

public sealed class MarkWithdrawalPaidHandler(
    IWalletWithdrawalRepository withdrawalRepository,
    IWalletRepository walletRepository,
    IDistributedLock distributedLock,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : ICommandHandler<MarkWithdrawalPaidCommand, Unit>
{
    private static readonly TimeSpan WalletLockExpiry = TimeSpan.FromSeconds(10);

    public async Task<ServiceResult<Unit>> Handle(
        MarkWithdrawalPaidCommand request,
        CancellationToken ct)
    {
        var withdrawalId = WalletWithdrawalRequestId.From(request.WithdrawalId);
        var adminId = UserId.From(currentUserService.UserId!.Value);

        var withdrawalForLookupResult = await (withdrawalRepository.GetByIdForUpdateAsync(withdrawalId, ct)).OrNotFoundAsync("درخواست برداشت یافت نشد.");
        if (withdrawalForLookupResult.IsFailure) return withdrawalForLookupResult.Error;
        var withdrawalForLookup = withdrawalForLookupResult.Value;

        var userId = withdrawalForLookup.UserId;

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"wallet:{userId.Value:N}",
            WalletLockExpiry,
            ct);

        if (lockHandle is null || !lockHandle.IsAcquired)
            return ServiceResult<Unit>.Conflict("عملیات دیگری روی کیف پول در حال انجام است. لطفاً مجدداً تلاش کنید.");

        try
        {
            var withdrawalResult = await (withdrawalRepository.GetByIdForUpdateAsync(withdrawalId, ct)).OrNotFoundAsync("درخواست برداشت یافت نشد.");
            if (withdrawalResult.IsFailure) return withdrawalResult.Error;
            var withdrawal = withdrawalResult.Value;

            var walletResult = await (walletRepository.GetByUserIdForUpdateAsync(withdrawal.UserId, ct)).OrNotFoundAsync("کیف پول کاربر یافت نشد.");
            if (walletResult.IsFailure) return walletResult.Error;
            var wallet = walletResult.Value;

            var now = dateTimeProvider.UtcNow;
            wallet.ReleaseReservation(withdrawal.ReservationId, now);
            wallet.Debit(
                withdrawal.Amount,
                $"برداشت به شماره پیگیری {request.BankReferenceNumber}",
                withdrawal.Id.Value.ToString(),
                now);

            withdrawal.MarkPaid(adminId, request.BankReferenceNumber, now);

            walletRepository.Update(wallet);
            withdrawalRepository.Update(withdrawal);

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
