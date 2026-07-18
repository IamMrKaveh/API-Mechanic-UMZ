using Application.Wallet.Features.Shared;
using Application.Wallet.Options;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Microsoft.Extensions.Options;

namespace Application.Wallet.Features.Queries.PreviewWalletTransfer;

public sealed class PreviewWalletTransferHandler(
    IUserRepository userRepository,
    IWalletRepository walletRepository,
    IWalletTransferRepository transferRepository,
    IOptions<WalletTransferOptions> options,
    ICurrentUserService currentUserService)
    : IRequestHandler<PreviewWalletTransferQuery, ServiceResult<WalletTransferPreviewDto>>
{
    private readonly WalletTransferOptions _options = options.Value;

    public async Task<ServiceResult<WalletTransferPreviewDto>> Handle(
        PreviewWalletTransferQuery request,
        CancellationToken ct)
    {
        var fromUserId = UserId.From(currentUserService.UserId.Value);

        PhoneNumber recipientPhone;
        try
        {
            recipientPhone = PhoneNumber.Create(request.RecipientPhoneNumber);
        }
        catch (DomainException ex)
        {
            return ServiceResult<WalletTransferPreviewDto>.Failure(ex.Message);
        }

        var recipient = await userRepository.GetByPhoneNumberAsync(recipientPhone, ct);
        if (recipient is null)
            return ServiceResult<WalletTransferPreviewDto>.NotFound("کاربری با این شماره یافت نشد.");

        if (recipient.Id.Equals(fromUserId))
            return ServiceResult<WalletTransferPreviewDto>.Failure("انتقال به کیف پول خود مجاز نیست.");

        if (!recipient.IsActive)
            return ServiceResult<WalletTransferPreviewDto>.Failure("حساب کاربری گیرنده غیرفعال است.");

        var senderWallet = await walletRepository.GetByUserIdAsync(fromUserId, ct);
        var senderAvailable = senderWallet?.AvailableBalance.Amount ?? 0m;

        var recipientWallet = await walletRepository.GetByUserIdAsync(recipient.Id, ct);
        if (recipientWallet is not null && !recipientWallet.IsActive)
            return ServiceResult<WalletTransferPreviewDto>.Failure("کیف پول گیرنده در حال حاضر مسدود است.");

        var today = DateTime.UtcNow.Date;
        var alreadyToday = await transferRepository.SumCompletedAmountForDayAsync(fromUserId, today, ct);
        var remaining = Math.Max(0m, _options.DailyLimit - alreadyToday);
        var canProceed = request.Amount <= senderAvailable
                         && request.Amount <= remaining
                         && request.Amount >= _options.MinimumAmount;

        string? warning = null;
        if (request.Amount > senderAvailable)
            warning = "موجودی قابل برداشت کافی نیست.";
        else if (request.Amount > remaining)
            warning = "مبلغ درخواستی از سقف روزانه انتقال بیشتر است.";
        else if (request.Amount < _options.MinimumAmount)
            warning = $"حداقل مبلغ انتقال {_options.MinimumAmount:N0} تومان است.";

        var displayName = BuildDisplayName(recipient);
        var maskedPhone = MaskPhone(recipient.PhoneNumber?.Value ?? recipientPhone.Value);

        return ServiceResult<WalletTransferPreviewDto>.Success(new WalletTransferPreviewDto
        {
            RecipientUserId = recipient.Id.Value,
            RecipientDisplayName = displayName,
            RecipientPhoneMasked = maskedPhone,
            Amount = request.Amount,
            SenderAvailableBalance = senderAvailable,
            DailyLimit = _options.DailyLimit,
            AlreadyTransferredToday = alreadyToday,
            RemainingDailyLimit = remaining,
            CanProceed = canProceed,
            Warning = warning
        });
    }

    private static string BuildDisplayName(Domain.User.Aggregates.User user)
    {
        var full = user.FullName?.ToString();
        if (!string.IsNullOrWhiteSpace(full))
            return full!;
        var phone = user.PhoneNumber?.Value;
        return string.IsNullOrWhiteSpace(phone) ? "کاربر" : MaskPhone(phone!);
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