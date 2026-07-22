namespace Infrastructure.Localization.Resources;

public static class ErrorMessages
{
    public static readonly IReadOnlyDictionary<string, string> Fa = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["error.general.unexpected"] = "خطای غیرمنتظره‌ای رخ داده است.",
        ["error.general.validation"] = "اطلاعات ورودی نامعتبر است.",
        ["error.general.not_found"] = "مورد درخواستی یافت نشد.",
        ["error.general.conflict"] = "داده تکراری یا وضعیت متعارض است.",
        ["error.general.unauthorized"] = "دسترسی غیرمجاز.",
        ["error.general.forbidden"] = "شما اجازه انجام این عملیات را ندارید.",
        ["error.general.rate_limit"] = "درخواست‌های شما بیش از حد مجاز است. لطفاً بعداً تلاش کنید.",
        ["error.general.concurrency"] = "تغییرات همزمان رخ داده است. لطفاً دوباره تلاش کنید.",
        ["error.general.cancelled"] = "درخواست لغو شد.",
        ["error.order.not_found"] = "سفارش یافت نشد.",
        ["error.order.already_paid"] = "سفارش قبلاً پرداخت شده است.",
        ["error.payment.invalid_amount"] = "مبلغ پرداخت نامعتبر است.",
        ["error.payment.expired"] = "تراکنش پرداخت منقضی شده است.",
        ["error.payment.transaction_not_found"] = "تراکنش پیدا نشد.",
        ["error.wallet.insufficient_balance"] = "کیف پول موجودی کافی ندارد.",
        ["error.wallet.transfer_limit_exceeded"] = "سقف انتقال روزانه پر شده است.",
        ["error.wallet.otp_mismatch"] = "کد تأیید نادرست است.",
        ["error.user.invalid_phone"] = "شماره تلفن نامعتبر است. شماره باید با ۰۹ شروع شود.",
        ["error.security.invalid_otp"] = "کد OTP وارد شده نامعتبر است.",
        ["error.brand.duplicate_name"] = "برند با این نام قبلاً وجود دارد.",
        ["error.category.duplicate_name"] = "دسته‌بندی با این نام قبلاً وجود دارد.",
        ["error.attribute.not_found"] = "ویژگی یافت نشد.",
        ["error.attribute.duplicate"] = "ویژگی با این نام قبلاً وجود دارد.",
        ["error.inventory.insufficient_stock"] = "موجودی کافی نیست."
    };

    public static readonly IReadOnlyDictionary<string, string> En = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["error.general.unexpected"] = "An unexpected error has occurred.",
        ["error.general.validation"] = "The provided input is invalid.",
        ["error.general.not_found"] = "The requested item was not found.",
        ["error.general.conflict"] = "The item already exists or its state is in conflict.",
        ["error.general.unauthorized"] = "Unauthorized access.",
        ["error.general.forbidden"] = "You are not allowed to perform this operation.",
        ["error.general.rate_limit"] = "You have exceeded the allowed request rate. Please try again later.",
        ["error.general.concurrency"] = "A concurrent change has occurred. Please try again.",
        ["error.general.cancelled"] = "The request was cancelled.",
        ["error.order.not_found"] = "Order not found.",
        ["error.order.already_paid"] = "Order has already been paid.",
        ["error.payment.invalid_amount"] = "The payment amount is invalid.",
        ["error.payment.expired"] = "The payment transaction has expired.",
        ["error.payment.transaction_not_found"] = "Transaction not found.",
        ["error.wallet.insufficient_balance"] = "Wallet has insufficient balance.",
        ["error.wallet.transfer_limit_exceeded"] = "Daily transfer limit exceeded.",
        ["error.wallet.otp_mismatch"] = "Verification code is incorrect.",
        ["error.user.invalid_phone"] = "The phone number is invalid. It must start with 09.",
        ["error.security.invalid_otp"] = "The provided OTP is invalid.",
        ["error.brand.duplicate_name"] = "A brand with this name already exists.",
        ["error.category.duplicate_name"] = "A category with this name already exists.",
        ["error.attribute.not_found"] = "Attribute not found.",
        ["error.attribute.duplicate"] = "An attribute with this name already exists.",
        ["error.inventory.insufficient_stock"] = "Insufficient stock."
    };

    public static IReadOnlyDictionary<string, string> ForCulture(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return Fa;

        return cultureName.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? En : Fa;
    }
}
