using Application.Wallet.Features.Shared;
using Application.Wallet.Options;
using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Microsoft.Extensions.Options;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Wallet.Features.Commands.InitiateWalletTransfer;

public sealed class InitiateWalletTransferHandler(
    IUserRepository userRepository,
    IWalletRepository walletRepository,
    IWalletTransferRepository transferRepository,
    IOtpService otpService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IOptions<WalletTransferOptions> options)
    : IRequestHandler<InitiateWalletTransferCommand, ServiceResult<InitiateWalletTransferResultDto>>
{
    private readonly WalletTransferOptions _options = options.Value;

    public async Task<ServiceResult<InitiateWalletTransferResultDto>> Handle(
        InitiateWalletTransferCommand request,
        CancellationToken ct)
    {
        try
        {
            var fromUserId = UserId.From(request.FromUserId);
            var recipientPhone = PhoneNumber.Create(request.RecipientPhoneNumber);

            var sender = await userRepository.GetByIdAsync(fromUserId, ct);
            if (sender is null || sender.PhoneNumber is null)
                return ServiceResult<InitiateWalletTransferResultDto>.Failure("اطلاعات کاربر جهت ارسال کد تأیید کامل نیست.");

            var recipient = await userRepository.GetByPhoneNumberAsync(recipientPhone, ct);
            if (recipient is null)
                return ServiceResult<InitiateWalletTransferResultDto>.NotFound("کاربری با این شماره یافت نشد.");

            if (recipient.Id.Equals(fromUserId))
                return ServiceResult<InitiateWalletTransferResultDto>.Failure("انتقال به کیف پول خود مجاز نیست.");

            if (!recipient.IsActive)
                return ServiceResult<InitiateWalletTransferResultDto>.Failure("حساب کاربری گیرنده غیرفعال است.");

            var senderWallet = await walletRepository.GetByUserIdAsync(fromUserId, ct);
            if (senderWallet is null)
                return ServiceResult<InitiateWalletTransferResultDto>.NotFound("کیف پول شما یافت نشد.");

            if (!senderWallet.IsActive)
                return ServiceResult<InitiateWalletTransferResultDto>.Failure("کیف پول شما در حال حاضر مسدود است.");

            var amount = Money.Create(request.Amount, _options.Currency);

            if (amount.Amount < _options.MinimumAmount)
                return ServiceResult<InitiateWalletTransferResultDto>.Failure(
                    $"حداقل مبلغ انتقال {_options.MinimumAmount:N0} تومان است.");

            if (amount.Amount > _options.MaximumAmount)
                return ServiceResult<InitiateWalletTransferResultDto>.Failure(
                    $"حداکثر مبلغ انتقال {_options.MaximumAmount:N0} تومان است.");

            if (senderWallet.AvailableBalance.IsLessThan(amount))
                return ServiceResult<InitiateWalletTransferResultDto>.Failure(
                    "موجودی قابل برداشت کافی نیست.");

            var today = dateTimeProvider.UtcNow.Date;
            var alreadyToday = await transferRepository.SumCompletedAmountForDayAsync(fromUserId, today, ct);
            if (alreadyToday + amount.Amount > _options.DailyLimit)
                return ServiceResult<InitiateWalletTransferResultDto>.Failure(
                    $"مجموع انتقال روزانه از سقف مجاز ({_options.DailyLimit:N0} تومان) عبور می‌کند.");

            var recentPending = await transferRepository.CountRecentPendingByUserAsync(
                fromUserId, TimeSpan.FromHours(1), ct);
            if (recentPending >= _options.MaxPendingTransfersPerHour)
                return ServiceResult<InitiateWalletTransferResultDto>.Conflict(
                    "تعداد درخواست‌های انتقال شما در یک ساعت اخیر بیش از حد مجاز است.");

            var otpCode = OtpCode.Generate(_options.OtpLength);
            var otpHash = otpService.HashOtp(otpCode);
            var otpTtl = TimeSpan.FromSeconds(_options.OtpTtlSeconds);

            var transfer = WalletTransfer.Initiate(
                fromUserId,
                recipient.Id,
                amount,
                otpHash,
                otpTtl,
                request.Description);

            await transferRepository.AddAsync(transfer, ct);

            var sendResult = await otpService.SendOtpAsync(
                sender.PhoneNumber,
                otpCode,
                OtpPurpose.Login,
                ct);

            if (sendResult.IsFailed)
            {
                transfer.MarkFailed(sendResult.Error ?? "ارسال کد تأیید ناموفق بود.");
                transferRepository.Update(transfer);
                await unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<InitiateWalletTransferResultDto>.Failure(
                    sendResult.Error ?? "ارسال کد تأیید ناموفق بود.");
            }

            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<InitiateWalletTransferResultDto>.Success(new InitiateWalletTransferResultDto
            {
                TransferId = transfer.Id.Value,
                SenderPhoneMasked = MaskPhone(sender.PhoneNumber.Value),
                OtpExpiresAt = transfer.OtpExpiresAt,
                OtpTtlSeconds = _options.OtpTtlSeconds,
                OtpLength = _options.OtpLength
            });
        }
        catch (WalletTransferLimitExceededException ex)
        {
            return ServiceResult<InitiateWalletTransferResultDto>.Failure(ex.Message);
        }
        catch (InvalidWalletTransferException ex)
        {
            return ServiceResult<InitiateWalletTransferResultDto>.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            return ServiceResult<InitiateWalletTransferResultDto>.Failure(ex.Message);
        }
    }

    private static string MaskPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 7)
            return phone ?? string.Empty;
        var start = phone[..4];
        var end = phone[^3..];
        return $"{start}****{end}";
    }
}