using SharedKernel.Localization;

namespace Presentation.Localization;

public static class WalletErrorTranslator
{
    private static readonly IReadOnlyDictionary<string, Func<IReadOnlyDictionary<string, object?>, string>> Templates =
        new Dictionary<string, Func<IReadOnlyDictionary<string, object?>, string>>
        {
            [DomainErrorCodes.Wallet.TopUpUnknownFailure] = _ => "خطای نامشخص در پرداخت.",
            [DomainErrorCodes.Wallet.TopUpInvalidState] = a => $"TopUp در وضعیت '{a.GetOrDefault("status", "?")}' قابل تغییر نیست.",

            [DomainErrorCodes.Wallet.TransferSelfNotAllowed] = _ => "انتقال به کیف پول خود مجاز نیست.",
            [DomainErrorCodes.Wallet.TransferMinimumAmount] = a => $"حداقل مبلغ انتقال {a.GetOrDefault("minimum", 0):N0} تومان است.",
            [DomainErrorCodes.Wallet.TransferInvalidOtpTtl] = _ => "مدت اعتبار کد تأیید نامعتبر است.",
            [DomainErrorCodes.Wallet.TransferOtpExpired] = _ => "مهلت وارد کردن کد تأیید به پایان رسیده است.",
            [DomainErrorCodes.Wallet.TransferOtpAttemptsExceeded] = _ => "تعداد تلاش‌های نامعتبر برای وارد کردن کد تأیید بیش از حد مجاز است.",
            [DomainErrorCodes.Wallet.TransferOnlyCreatorCanCancel] = _ => "فقط ایجادکننده انتقال می‌تواند آن را لغو کند.",
            [DomainErrorCodes.Wallet.TransferInvalidStateForCancel] = a => $"انتقال در وضعیت '{a.GetOrDefault("status", "?")}' قابل لغو نیست.",
            [DomainErrorCodes.Wallet.TransferInvalidState] = a => $"انتقال در وضعیت '{a.GetOrDefault("status", "?")}' قابل تغییر نیست.",

            [DomainErrorCodes.Wallet.FraudAlertInvalidStateForReview] = a => $"Fraud alert در وضعیت '{a.GetOrDefault("status", "?")}' قابل بررسی نیست.",
            [DomainErrorCodes.Wallet.FraudAlertInvalidStateForDismiss] = a => $"Fraud alert در وضعیت '{a.GetOrDefault("status", "?")}' قابل رد کردن نیست.",

            [DomainErrorCodes.Wallet.WithdrawalUserIdRequired] = _ => "شناسه کاربر الزامی است.",
            [DomainErrorCodes.Wallet.WithdrawalAmountRequired] = _ => "مبلغ الزامی است.",
            [DomainErrorCodes.Wallet.WithdrawalIbanRequired] = _ => "شماره شبا الزامی است.",
            [DomainErrorCodes.Wallet.WithdrawalAccountHolderRequired] = _ => "نام صاحب حساب الزامی است.",
            [DomainErrorCodes.Wallet.WithdrawalReservationIdRequired] = _ => "شناسه رزرو الزامی است.",
            [DomainErrorCodes.Wallet.WithdrawalMinimumAmount] = a => $"حداقل مبلغ برداشت {a.GetOrDefault("minimum", 0):N0} تومان است.",
            [DomainErrorCodes.Wallet.WithdrawalRejectionReasonRequired] = _ => "دلیل رد درخواست الزامی است.",
            [DomainErrorCodes.Wallet.WithdrawalInvalidStateForPay] = a => $"درخواست برداشت در وضعیت '{a.GetOrDefault("status", "?")}' قابل پرداخت نیست.",
            [DomainErrorCodes.Wallet.WithdrawalBankReferenceRequired] = _ => "شماره پیگیری بانکی الزامی است.",
            [DomainErrorCodes.Wallet.WithdrawalOnlyOwnerCanCancel] = _ => "فقط صاحب درخواست می‌تواند آن را لغو کند.",
            [DomainErrorCodes.Wallet.WithdrawalInvalidStateForAction] = a => $"درخواست در وضعیت '{a.GetOrDefault("status", "?")}' قابل {a.GetOrDefault("action", "?")} نیست.",

            [DomainErrorCodes.Wallet.InsufficientBalance] = a => $"کیف پول '{a.GetOrDefault("walletId", "?")}' موجودی کافی ندارد. درخواستی: {a.GetOrDefault("requestedAmount", 0):N0} {a.GetOrDefault("requestedCurrency", "")}, موجود: {a.GetOrDefault("availableAmount", 0):N0} {a.GetOrDefault("availableCurrency", "")}.",
            [DomainErrorCodes.Wallet.Inactive] = a => $"کیف پول '{a.GetOrDefault("walletId", "?")}' غیرفعال است و قادر به پردازش تراکنش نیست.",
            [DomainErrorCodes.Wallet.InvalidAmount] = a => $"مبلغ تراکنش کیف پول '{a.GetOrDefault("amount", 0)}' نامعتبر است. مبلغ باید بزرگ‌تر از صفر باشد.",
            [DomainErrorCodes.Wallet.TransferLimitExceeded] = a => $"سقف انتقال روزانه ({a.GetOrDefault("dailyLimit", 0):N0} تومان) پر شده است. مجموع انتقال‌های امروز: {a.GetOrDefault("alreadyTransferredToday", 0):N0} تومان.",
            [DomainErrorCodes.Wallet.TransferOtpMismatch] = a => $"کد تأیید نادرست است. {a.GetOrDefault("remainingAttempts", 0)} تلاش دیگر باقی مانده است.",
            [DomainErrorCodes.Wallet.TransferOtpAttemptsUsed] = _ => "تعداد تلاش‌های مجاز به پایان رسیده است.",
            [DomainErrorCodes.Wallet.InvalidTopUpAmount] = a => $"مبلغ شارژ ({a.GetOrDefault("providedAmount", 0):N0}) کمتر از حداقل مجاز ({a.GetOrDefault("minimumAmount", 0):N0} تومان) است.",
            [DomainErrorCodes.Wallet.DebitRequestInvalidStatus] = a => $"وضعیت فعلی درخواست ({a.GetOrDefault("currentStatus", "?")}) اجازه این عملیات را نمی‌دهد.",
            [DomainErrorCodes.Wallet.DebitApprovalUnauthorized] = _ => "فقط صاحب کیف پول می‌تواند این درخواست را تایید یا رد کند.",
            [DomainErrorCodes.Wallet.DebitRequestExpired] = _ => "مهلت پاسخ به این درخواست به پایان رسیده است.",
            [DomainErrorCodes.Wallet.DebitRequestNotFound] = a => $"درخواست کسر با شناسه {a.GetOrDefault("requestId", "?")} یافت نشد.",
            [DomainErrorCodes.Wallet.ReservationNotFound] = a => $"رزرو کیف پول با شناسه '{a.GetOrDefault("reservationId", "?")}' یافت نشد."
        };

    public static string Translate(DomainException exception)
    {
        if (exception is null) return string.Empty;
        return Templates.TryGetValue(exception.ErrorCode, out var template)
            ? template(exception.Args)
            : exception.Message;
    }

    private static object GetOrDefault(this IReadOnlyDictionary<string, object?> dict, string key, object fallback)
        => dict.TryGetValue(key, out var value) && value is not null ? value : fallback;
}
