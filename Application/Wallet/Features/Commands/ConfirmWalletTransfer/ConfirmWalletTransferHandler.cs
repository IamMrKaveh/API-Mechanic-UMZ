using Application.Auth.Contracts;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Wallet.Features.Shared;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using MediatR;

namespace Application.Wallet.Features.Commands.ConfirmWalletTransfer;

public sealed class ConfirmWalletTransferHandler(
    IWalletTransferRepository transferRepository,
    IWalletRepository walletRepository,
    IUserRepository userRepository,
    IOtpService otpService,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : IRequestHandler<ConfirmWalletTransferCommand, ServiceResult<ConfirmWalletTransferResultDto>>
{
    public async Task<ServiceResult<ConfirmWalletTransferResultDto>> Handle(
        ConfirmWalletTransferCommand request,
        CancellationToken ct)
    {
        try
        {
            var transferId = WalletTransferId.From(request.TransferId);
            var fromUserId = UserId.From(request.FromUserId);

            var transfer = await transferRepository.GetByIdForUpdateAsync(transferId, ct);
            if (transfer is null)
                return ServiceResult<ConfirmWalletTransferResultDto>.NotFound("درخواست انتقال یافت نشد.");

            if (!transfer.FromUserId.Equals(fromUserId))
                return ServiceResult<ConfirmWalletTransferResultDto>.Forbidden("دسترسی به این درخواست انتقال مجاز نیست.");

            if (transfer.Status != WalletTransferStatus.PendingOtp)
                return ServiceResult<ConfirmWalletTransferResultDto>.Failure(
                    $"درخواست انتقال در وضعیت '{transfer.Status}' قابل تأیید نیست.");

            OtpCode otpCode;
            try
            {
                otpCode = OtpCode.Create(request.OtpCode);
            }
            catch (DomainException)
            {
                return ServiceResult<ConfirmWalletTransferResultDto>.Failure("کد تأیید نامعتبر است.");
            }

            var hash = otpService.HashOtp(otpCode);

            try
            {
                transfer.VerifyOtp(hash);
            }
            catch (WalletTransferOtpMismatchException ex)
            {
                transferRepository.Update(transfer);
                await unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<ConfirmWalletTransferResultDto>.Failure(ex.Message);
            }
            catch (InvalidWalletTransferException ex)
            {
                transferRepository.Update(transfer);
                await unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<ConfirmWalletTransferResultDto>.Failure(ex.Message);
            }

            var senderWallet = await walletRepository.GetByUserIdForUpdateAsync(transfer.FromUserId, ct);
            if (senderWallet is null)
            {
                transfer.MarkFailed("کیف پول فرستنده یافت نشد.");
                transferRepository.Update(transfer);
                await unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<ConfirmWalletTransferResultDto>.Failure("کیف پول فرستنده یافت نشد.");
            }

            var recipientWallet = await walletRepository.GetByUserIdForUpdateAsync(transfer.ToUserId, ct);
            if (recipientWallet is null)
            {
                recipientWallet = Domain.Wallet.Aggregates.Wallet.Create(
                    transfer.ToUserId,
                    transfer.Amount.Currency);
                await walletRepository.AddAsync(recipientWallet, ct);
            }
            else if (!recipientWallet.IsActive)
            {
                transfer.MarkFailed("کیف پول گیرنده در حال حاضر مسدود است.");
                transferRepository.Update(transfer);
                await unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<ConfirmWalletTransferResultDto>.Failure("کیف پول گیرنده در حال حاضر مسدود است.");
            }

            try
            {
                if (senderWallet.Id.Value.CompareTo(recipientWallet.Id.Value) < 0)
                {
                    senderWallet.Debit(transfer.Amount, BuildDebitDescription(transfer), transfer.CorrelationId);
                    recipientWallet.Credit(transfer.Amount, BuildCreditDescription(transfer), transfer.CorrelationId);
                }
                else
                {
                    recipientWallet.Credit(transfer.Amount, BuildCreditDescription(transfer), transfer.CorrelationId);
                    senderWallet.Debit(transfer.Amount, BuildDebitDescription(transfer), transfer.CorrelationId);
                }
            }
            catch (InsufficientWalletBalanceException ex)
            {
                transfer.MarkFailed(ex.Message);
                transferRepository.Update(transfer);
                await unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<ConfirmWalletTransferResultDto>.Failure(ex.Message);
            }
            catch (WalletInactiveException ex)
            {
                transfer.MarkFailed(ex.Message);
                transferRepository.Update(transfer);
                await unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<ConfirmWalletTransferResultDto>.Failure(ex.Message);
            }

            walletRepository.Update(senderWallet);
            walletRepository.Update(recipientWallet);

            transfer.MarkCompleted();
            transferRepository.Update(transfer);

            await unitOfWork.SaveChangesAsync(ct);

            var recipient = await userRepository.GetByIdAsync(transfer.ToUserId, ct);
            var recipientName = BuildDisplayName(recipient);

            await auditService.LogSecurityEventAsync(
                "WalletTransferCompleted",
                $"انتقال {transfer.Amount.Amount:N0} از {transfer.FromUserId.Value} به {transfer.ToUserId.Value} تکمیل شد.",
                IpAddress.Unknown,
                transfer.FromUserId,
                ct);

            return ServiceResult<ConfirmWalletTransferResultDto>.Success(new ConfirmWalletTransferResultDto
            {
                TransferId = transfer.Id.Value,
                Status = transfer.Status.ToString(),
                Amount = transfer.Amount.Amount,
                RecipientDisplayName = recipientName,
                CorrelationId = transfer.CorrelationId,
                CompletedAt = transfer.CompletedAt ?? DateTime.UtcNow
            });
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WalletTransferConcurrencyConflict",
                $"تعارض همزمانی در تأیید انتقال {request.TransferId}.",
                ct);
            return ServiceResult<ConfirmWalletTransferResultDto>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<ConfirmWalletTransferResultDto>.Failure(ex.Message);
        }
    }

    private static string BuildDebitDescription(Domain.Wallet.Aggregates.WalletTransfer transfer)
        => string.IsNullOrWhiteSpace(transfer.Description)
            ? $"انتقال به کاربر {transfer.ToUserId.Value}"
            : $"انتقال به کاربر {transfer.ToUserId.Value} - {transfer.Description}";

    private static string BuildCreditDescription(Domain.Wallet.Aggregates.WalletTransfer transfer)
        => string.IsNullOrWhiteSpace(transfer.Description)
            ? $"دریافت از کاربر {transfer.FromUserId.Value}"
            : $"دریافت از کاربر {transfer.FromUserId.Value} - {transfer.Description}";

    private static string BuildDisplayName(Domain.User.Aggregates.User? user)
    {
        if (user is null) return "کاربر";
        var full = user.FullName?.ToString();
        if (!string.IsNullOrWhiteSpace(full)) return full!;
        var phone = user.PhoneNumber?.Value;
        return string.IsNullOrWhiteSpace(phone) ? "کاربر" : phone!;
    }
}