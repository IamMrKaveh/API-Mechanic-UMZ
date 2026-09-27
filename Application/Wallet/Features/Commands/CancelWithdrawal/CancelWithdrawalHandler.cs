using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Wallet.Features.Commands.CancelWithdrawal;

public sealed class CancelWithdrawalHandler(
    IWalletWithdrawalRepository withdrawalRepository,
    IWalletRepository walletRepository,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : ICommandHandler<CancelWithdrawalCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(
        CancelWithdrawalCommand request,
        CancellationToken ct)
    {
        try
        {
            var withdrawalId = WalletWithdrawalRequestId.From(request.WithdrawalId);
            var userId = UserId.From(currentUserService.UserId!.Value);

            var withdrawalResult = await (withdrawalRepository.GetByIdForUpdateAsync(withdrawalId, ct)).OrNotFoundAsync("درخواست برداشت یافت نشد.");
            if (withdrawalResult.IsFailure) return withdrawalResult.Error;
            var withdrawal = withdrawalResult.Value;

            if (!withdrawal.UserId.Equals(userId))
                return ServiceResult<Unit>.Failure("شما مجاز به لغو این درخواست نیستید.");

            var walletResult = await (walletRepository.GetByUserIdForUpdateAsync(userId, ct)).OrNotFoundAsync("کیف پول یافت نشد.");
            if (walletResult.IsFailure) return walletResult.Error;
            var wallet = walletResult.Value;

            var now = dateTimeProvider.UtcNow;
            wallet.ReleaseReservation(withdrawal.ReservationId, now);

            withdrawal.Cancel(userId, now);

            walletRepository.Update(wallet);
            withdrawalRepository.Update(withdrawal);

            await auditService.LogSecurityEventAsync(
                "WithdrawalCancelled",
                $"درخواست برداشت {withdrawalId.Value} توسط کاربر لغو شد.",
                IpAddress.Unknown,
                userId,
                ct);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}
