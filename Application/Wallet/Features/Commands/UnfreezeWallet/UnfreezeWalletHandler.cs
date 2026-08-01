using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.UnfreezeWallet;

public sealed class UnfreezeWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<UnfreezeWalletCommand, Unit>
{
    private const string ManualUnfreezeReason = "[ADMIN-MANUAL]";

    public async Task<ServiceResult<Unit>> Handle(UnfreezeWalletCommand request, CancellationToken ct)
    {
        try
        {
            var userId = UserId.From(request.UserId);
            var adminId = UserId.From(currentUserService.UserId.Value);

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
            {
                wallet = Domain.Wallet.Aggregates.Wallet.Create(userId);
                await walletRepository.AddAsync(wallet, ct);
                await unitOfWork.SaveChangesAsync(ct);

                await auditService.LogSystemEventAsync(
                    "WalletAutoCreatedOnUnfreeze",
                    $"کیف پول کاربر {userId.Value} به‌صورت خودکار در حین رفع مسدودی ایجاد شد. ادمین: {adminId.Value}.",
                    ct);

                return ServiceResult<Unit>.Success(Unit.Value);
            }

            wallet.Unfreeze(adminId, ManualUnfreezeReason);
            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSystemEventAsync(
                "WalletUnfrozen",
                $"کیف پول کاربر {userId.Value} توسط ادمین {adminId.Value} رفع مسدودی شد. علت: {ManualUnfreezeReason}.",
                ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WalletUnfreezeConcurrencyConflict",
                $"تعارض همزمانی در رفع مسدودی کیف پول کاربر {request.UserId}.",
                ct);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}
