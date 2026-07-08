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
        UserId userId;
        IbanNumber iban;
        Money amount;

        try
        {
            userId = UserId.From(request.UserId);
        }
        catch (DomainException)
        {
            return ServiceResult<Guid>.Validation("شناسه کاربر نامعتبر است.");
        }

        try
        {
            iban = IbanNumber.Create(request.Iban);
        }
        catch (DomainException ex)
        {
            return ServiceResult<Guid>.Validation(ex.Message);
        }

        try
        {
            amount = Money.Create(request.Amount);
        }
        catch (DomainException ex)
        {
            return ServiceResult<Guid>.Validation(ex.Message);
        }

        if (string.IsNullOrWhiteSpace(request.AccountHolder))
            return ServiceResult<Guid>.Validation("نام صاحب حساب الزامی است.");

        try
        {
            var pendingCount = await withdrawalRepository
                .CountByUserAndStatusAsync(userId, WithdrawalStatus.Pending, ct);

            if (pendingCount >= MaxPendingPerUser)
                return ServiceResult<Guid>.Conflict(
                    $"شما بیش از {MaxPendingPerUser} درخواست برداشت در انتظار بررسی دارید.");

            var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
            if (wallet is null)
            {
                wallet = Domain.Wallet.Aggregates.Wallet.Create(userId);
                await walletRepository.AddAsync(wallet, ct);
            }

            if (!wallet.IsActive)
                return ServiceResult<Guid>.Failure("کیف پول شما غیرفعال است.");

            if (wallet.AvailableBalance.IsLessThan(amount))
                return ServiceResult<Guid>.Failure(
                    $"موجودی قابل برداشت کافی نیست. موجودی فعلی: {wallet.AvailableBalance.Amount:N0} تومان.");

            var reservationId = WalletReservationId.NewId();
            wallet.CreateReservation(reservationId, amount, "withdrawal-request");

            var withdrawal = WalletWithdrawalRequest.Create(
                userId, amount, iban, request.AccountHolder,
                reservationId, request.Description);

            walletRepository.Update(wallet);
            await withdrawalRepository.AddAsync(withdrawal, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<Guid>.Success(withdrawal.Id.Value);
        }
        catch (InsufficientWalletBalanceException ex)
        {
            return ServiceResult<Guid>.Failure(ex.Message);
        }
        catch (WalletInactiveException)
        {
            return ServiceResult<Guid>.Failure("کیف پول شما غیرفعال است.");
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WithdrawalRequestConcurrencyConflict",
                $"تعارض همزمانی در ثبت درخواست برداشت کاربر {request.UserId}", ct);
            return ServiceResult<Guid>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<Guid>.Validation(ex.Message);
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"[Withdrawal] Unexpected error for user {request.UserId}: {ex.Message}", ct);
            return ServiceResult<Guid>.Failure("خطا در ثبت درخواست برداشت.");
        }
    }
}