using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.MarkWithdrawalPaid;

public sealed class MarkWithdrawalPaidHandler(
    IWalletWithdrawalRepository withdrawalRepository,
    IWalletRepository walletRepository,
    ICurrentUserService currentUserService)
    : ICommandHandler<MarkWithdrawalPaidCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(
        MarkWithdrawalPaidCommand request,
        CancellationToken ct)
    {
        try
        {
            var withdrawalId = WalletWithdrawalRequestId.From(request.WithdrawalId);
            var adminId = UserId.From(currentUserService.UserId.Value);

            var withdrawal = await withdrawalRepository.GetByIdForUpdateAsync(withdrawalId, ct);
            if (withdrawal is null)
                return ServiceResult<Unit>.NotFound("درخواست برداشت یافت نشد.");

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(withdrawal.UserId, ct);
            if (wallet is null)
                return ServiceResult<Unit>.NotFound("کیف پول کاربر یافت نشد.");

            wallet.ReleaseReservation(withdrawal.ReservationId);
            wallet.Debit(
                withdrawal.Amount,
                $"برداشت به شماره پیگیری {request.BankReferenceNumber}",
                withdrawal.Id.Value.ToString());

            withdrawal.MarkPaid(adminId, request.BankReferenceNumber);

            walletRepository.Update(wallet);
            withdrawalRepository.Update(withdrawal);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
        catch (InsufficientWalletBalanceException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            return ServiceResult<Unit>.Failure(ex.Message);
        }
    }
}