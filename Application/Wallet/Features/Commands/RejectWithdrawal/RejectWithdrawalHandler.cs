using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.RejectWithdrawal;

public sealed class RejectWithdrawalHandler(
    IWalletWithdrawalRepository withdrawalRepository,
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : ICommandHandler<RejectWithdrawalCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(
        RejectWithdrawalCommand request,
        CancellationToken ct)
    {
        try
        {
            var withdrawalId = WalletWithdrawalRequestId.From(request.WithdrawalId);
            var adminId = UserId.From(request.AdminId);

            var withdrawal = await withdrawalRepository.GetByIdForUpdateAsync(withdrawalId, ct);
            if (withdrawal is null)
                return ServiceResult<Unit>.NotFound("درخواست برداشت یافت نشد.");

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(withdrawal.UserId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول کاربر یافت نشد.");

            wallet.ReleaseReservation(withdrawal.ReservationId);
            withdrawal.Reject(adminId, request.Reason);

            walletRepository.Update(wallet);
            withdrawalRepository.Update(withdrawal);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WithdrawalRejectConcurrencyConflict",
                $"تعارض همزمانی در رد درخواست برداشت {request.WithdrawalId}",
                ct);
            return ServiceResult<Unit>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}