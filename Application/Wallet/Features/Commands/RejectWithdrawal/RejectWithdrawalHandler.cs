using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Wallet.Features.Commands.RejectWithdrawal;

public sealed class RejectWithdrawalHandler(
    IWalletWithdrawalRepository withdrawalRepository,
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : ICommandHandler<RejectWithdrawalCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(
        RejectWithdrawalCommand request,
        CancellationToken ct)
    {
        try
        {
            var withdrawalId = WalletWithdrawalRequestId.From(request.WithdrawalId);
            var adminId = UserId.From(currentUserService.UserId!.Value);

            var withdrawalResult = await (withdrawalRepository.GetByIdForUpdateAsync(withdrawalId, ct)).OrNotFoundAsync("درخواست برداشت یافت نشد.");
            if (withdrawalResult.IsFailure) return withdrawalResult.Error;
            var withdrawal = withdrawalResult.Value;

            var walletResult = await (walletRepository.GetByUserIdForUpdateAsync(withdrawal.UserId, ct)).OrNotFoundAsync("کیف پول کاربر یافت نشد.");
            if (walletResult.IsFailure) return walletResult.Error;
            var wallet = walletResult.Value;

            var now = dateTimeProvider.UtcNow;
            wallet.ReleaseReservation(withdrawal.ReservationId, now);
            withdrawal.Reject(adminId, request.Reason, now);

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
