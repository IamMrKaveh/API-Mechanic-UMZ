using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Application.Wallet.Features.Commands.RequestWithdrawal;

public sealed class RequestWithdrawalHandler(
    IWalletRepository walletRepository,
    IWalletWithdrawalRepository withdrawalRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : ICommandHandler<RequestWithdrawalCommand, Guid>
{
    private const int MaxPendingPerUser = 5;

    public async Task<ServiceResult<Guid>> Handle(
        RequestWithdrawalCommand request,
        CancellationToken ct)
    {
        try
        {
            var userId = UserId.From(request.UserId);
            var iban = IbanNumber.Create(request.Iban);
            var amount = Money.Create(request.Amount);

            var pendingCount = await withdrawalRepository
                .CountByUserAndStatusAsync(userId, WithdrawalStatus.Pending, ct);

            if (pendingCount >= MaxPendingPerUser)
                return ServiceResult<Guid>.Conflict(
                    $"شما بیش از {MaxPendingPerUser} درخواست برداشت در انتظار بررسی دارید.");

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
                return ServiceResult<Guid>.NotFound("کیف پول یافت نشد.");

            var reservationId = WalletReservationId.NewId();
            wallet.CreateReservation(
                reservationId,
                amount,
                $"withdrawal-request");

            var withdrawal = WalletWithdrawalRequest.Create(
                userId,
                amount,
                iban,
                request.AccountHolder,
                reservationId,
                request.Description);

            walletRepository.Update(wallet);
            await withdrawalRepository.AddAsync(withdrawal, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Guid>.Success(withdrawal.Id.Value);
        }
        catch (InsufficientWalletBalanceException ex)
        {
            return ServiceResult<Guid>.Failure(ex.Message);
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WithdrawalRequestConcurrencyConflict",
                $"تعارض همزمانی در ثبت درخواست برداشت کاربر {request.UserId}",
                ct);
            return ServiceResult<Guid>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Guid>.Failure(ex.Message);
        }
        catch (Exception)
        {
            return ServiceResult<Guid>.Failure("خطا در ثبت درخواست برداشت.");
        }
    }
}