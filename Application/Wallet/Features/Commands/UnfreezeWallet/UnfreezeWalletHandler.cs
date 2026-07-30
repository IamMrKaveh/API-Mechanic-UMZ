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
                    $"کیف پول کاربر {userId.Value} به صورت خودکار در حین رفع مسدودی ایجاد شد.",
                    ct);

                return ServiceResult<Unit>.Success(Unit.Value);
            }

            wallet.Unfreeze(adminId);
            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSystemEventAsync(
                "WalletUnfrozen",
                $"کیف پول کاربر {userId.Value} توسط ادمین {adminId.Value} رفع مسدودی شد.",
                ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}
