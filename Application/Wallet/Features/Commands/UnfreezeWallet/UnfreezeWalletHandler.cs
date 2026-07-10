using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.UnfreezeWallet;

public sealed class UnfreezeWalletHandler(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : ICommandHandler<UnfreezeWalletCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(
        UnfreezeWalletCommand request,
        CancellationToken ct)
    {
        try
        {
            var userId = UserId.From(request.UserId);
            var adminId = UserId.From(request.AdminId);

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول کاربر یافت نشد.");

            wallet.Unfreeze(adminId);

            walletRepository.Update(wallet);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSystemEventAsync(
                "WalletUnfrozen",
                $"کیف پول کاربر {request.UserId} توسط ادمین {request.AdminId} رفع مسدودی شد.",
                ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WalletUnfreezeConcurrencyConflict",
                $"تعارض همزمانی در رفع مسدودی کیف پول کاربر {request.UserId}",
                ct);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}