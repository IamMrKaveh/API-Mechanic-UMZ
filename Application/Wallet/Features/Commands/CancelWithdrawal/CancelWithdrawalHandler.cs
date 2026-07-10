using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.CancelWithdrawal;

public sealed class CancelWithdrawalHandler(
    IWalletWithdrawalRepository withdrawalRepository,
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : ICommandHandler<CancelWithdrawalCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(
        CancelWithdrawalCommand request,
        CancellationToken ct)
    {
        try
        {
            var withdrawalId = WalletWithdrawalRequestId.From(request.WithdrawalId);
            var userId = UserId.From(request.UserId);

            var withdrawal = await withdrawalRepository.GetByIdForUpdateAsync(withdrawalId, ct);
            if (withdrawal is null)
                return ServiceResult<Unit>.NotFound("درخواست برداشت یافت نشد.");

            if (withdrawal.UserId != userId)
                return ServiceResult<Unit>.Failure("شما مجاز به لغو این درخواست نیستید.");

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول کاربر یافت نشد.");

            wallet.ReleaseReservation(withdrawal.ReservationId);
            withdrawal.Cancel();

            walletRepository.Update(wallet);
            withdrawalRepository.Update(withdrawal);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WithdrawalCancelConcurrencyConflict",
                $"تعارض همزمانی در لغو درخواست برداشت {request.WithdrawalId}",
                ct);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}