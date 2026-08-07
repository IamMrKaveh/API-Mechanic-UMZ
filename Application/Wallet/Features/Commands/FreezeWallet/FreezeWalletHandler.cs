using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.FreezeWallet;

public sealed class FreezeWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IDistributedLock distributedLock,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<FreezeWalletCommand, Unit>
{
    private static readonly TimeSpan WalletLockExpiry = TimeSpan.FromSeconds(10);

    public async Task<ServiceResult<Unit>> Handle(
        FreezeWalletCommand request,
        CancellationToken ct)
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
            var adminId = UserId.From(currentUserService.UserId!.Value);

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول کاربر یافت نشد.");

            wallet.Freeze(request.Reason, adminId);

            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSystemEventAsync(
                "WalletFrozen",
                $"کیف پول کاربر {userId.Value} توسط ادمین {adminId.Value} فریز شد. علت: {request.Reason}.",
                ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (WalletInactiveException)
        {
            return ServiceResult<Unit>.Conflict("کیف پول در حال حاضر مسدود است.");
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WalletFreezeConcurrencyConflict",
                $"تعارض همزمانی در فریز کیف پول کاربر {userId.Value}.",
                ct);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}
