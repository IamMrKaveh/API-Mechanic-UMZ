using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.ForceFreezeFromFraudAlert;

public sealed class ForceFreezeFromFraudAlertHandler(
    IWalletFraudAlertRepository alertRepository,
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IDistributedLock distributedLock,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<ForceFreezeFromFraudAlertCommand, Unit>
{
    private static readonly TimeSpan WalletLockExpiry = TimeSpan.FromSeconds(10);

    public async Task<ServiceResult<Unit>> Handle(
        ForceFreezeFromFraudAlertCommand request,
        CancellationToken ct)
    {
        try
        {
            var alertId = WalletFraudAlertId.From(request.AlertId);
            var adminId = UserId.From(currentUserService.UserId!.Value);

            var alert = await alertRepository.GetByIdAsync(alertId, ct);
            if (alert is null)
                return ServiceResult<Unit>.NotFound("هشدار مورد نظر یافت نشد.");

            if (alert.Status != FraudAlertStatus.Open)
                return ServiceResult<Unit>.Conflict(
                    $"این هشدار در وضعیت '{alert.Status}' است و امکان اعمال Force-Freeze وجود ندارد.");

            await using var lockHandle = await distributedLock.AcquireAsync(
                $"wallet:{alert.UserId.Value:N}", WalletLockExpiry, ct);

            if (lockHandle is null || !lockHandle.IsAcquired)
                return ServiceResult<Unit>.Conflict("عملیات دیگری روی کیف پول در حال انجام است. لطفاً مجدداً تلاش کنید.");

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(alert.UserId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول کاربر یافت نشد.");

            var reason = string.IsNullOrWhiteSpace(request.AdditionalNote)
                ? $"[Force-Freeze from Alert {alert.Id.Value:N}] {alert.RuleName}: {alert.Description}"
                : $"[Force-Freeze from Alert {alert.Id.Value:N}] {alert.RuleName}: {alert.Description} | Note: {request.AdditionalNote}";

            if (wallet.IsActive)
            {
                wallet.Freeze(reason, adminId);
                walletRepository.Update(wallet);
            }

            var reviewNote = string.IsNullOrWhiteSpace(request.AdditionalNote)
                ? "[FORCE-FREEZE-APPLIED]"
                : $"[FORCE-FREEZE-APPLIED] {request.AdditionalNote}";

            alert.MarkAsReviewed(adminId, reviewNote);
            alertRepository.Update(alert);

            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSystemEventAsync(
                "WalletForceFrozenFromFraudAlert",
                $"کیف پول کاربر {alert.UserId.Value} به‌واسطه‌ی هشدار {alert.Id.Value} توسط ادمین {adminId.Value} به‌صورت اجباری فریز و هشدار به‌عنوان بررسی‌شده ثبت شد. قانون: {alert.RuleName}.",
                ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (WalletInactiveException)
        {
            return ServiceResult<Unit>.Conflict("کیف پول از قبل مسدود است.");
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WalletForceFreezeConcurrencyConflict",
                $"تعارض همزمانی در Force-Freeze از هشدار {request.AlertId}.",
                ct);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}
