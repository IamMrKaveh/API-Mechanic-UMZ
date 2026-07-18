using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.CancelWithdrawal;

public sealed class CancelWithdrawalHandler(
    IWalletWithdrawalRepository withdrawalRepository,
    IWalletRepository walletRepository,
    IAuditService auditService,
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
            var userId = UserId.From(currentUserService.UserId.Value);

            var withdrawal = await withdrawalRepository.GetByIdForUpdateAsync(withdrawalId, ct);
            if (withdrawal is null)
                return ServiceResult<Unit>.NotFound("درخواست برداشت یافت نشد.");

            if (!withdrawal.UserId.Equals(userId))
                return ServiceResult<Unit>.Failure("شما مجاز به لغو این درخواست نیستید.");

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول یافت نشد.");

            wallet.ReleaseReservation(withdrawal.ReservationId);

            withdrawal.Cancel(userId);

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