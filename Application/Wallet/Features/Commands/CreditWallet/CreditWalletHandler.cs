using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.CreditWallet;

public class CreditWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IDistributedLock distributedLock,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreditWalletCommand, Unit>
{
    private const string DefaultCurrency = "IRT";
    private const string AutoUnfreezeReason = "[ADMIN-CREDIT-AUTO-UNFREEZE]";
    private static readonly TimeSpan WalletLockExpiry = TimeSpan.FromSeconds(10);

    public async Task<ServiceResult<Unit>> Handle(CreditWalletCommand request, CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"wallet:{userId.Value:N}",
            WalletLockExpiry,
            ct);

        if (lockHandle is null || !lockHandle.IsAcquired)
            return ServiceResult<Unit>.Conflict("عملیات دیگری روی کیف پول در حال انجام است. لطفاً مجدداً تلاش کنید.");

        try
        {
            var alreadyProcessed = await walletRepository.HasIdempotencyKeyAsync(userId, request.IdempotencyKey, ct);
            if (alreadyProcessed)
                return ServiceResult<Unit>.Success(Unit.Value);

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
            {
                wallet = Domain.Wallet.Aggregates.Wallet.Create(userId);
                await walletRepository.AddAsync(wallet, ct);
            }

            var autoUnfrozen = false;
            UserId? adminIdForUnfreeze = null;

            if (wallet.IsActive is false && currentUserService.IsAdmin)
            {
                adminIdForUnfreeze = UserId.From(currentUserService.UserId.Value);
                wallet.Unfreeze(adminIdForUnfreeze, AutoUnfreezeReason);
                autoUnfrozen = true;
            }

            var amount = Money.Create(request.Amount, DefaultCurrency);

            wallet.Credit(
                amount,
                request.Description ?? request.TransactionType.ToString(),
                request.ReferenceId,
                request.IdempotencyKey);

            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            if (autoUnfrozen && adminIdForUnfreeze is not null)
            {
                await auditService.LogSystemEventAsync(
                    "WalletAutoUnfrozenOnAdminCredit",
                    $"کیف پول کاربر {userId.Value} در حین شارژ ادمینی به‌صورت خودکار رفع مسدودی شد. ادمین: {adminIdForUnfreeze.Value}. علت: {AutoUnfreezeReason}.",
                    ct);
            }

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WalletCreditConcurrencyConflict",
                $"تعارض همزمانی در شارژ کیف پول کاربر {userId.Value}. IdempotencyKey: {request.IdempotencyKey}.",
                ct);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}
