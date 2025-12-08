namespace Infrastructure.Payment.ZarinPal;

public class ZarinPalErrorMapper
{
    private static readonly Dictionary<int, string> ErrorMessages = new()
    {
        { 100, "عملیات موفقیت‌آمیز بود." },
        { 101, "تراکنش قبلاً تایید شده است." },
        { -9, "اطلاعات ارسال شده ناقص است." },
        { -10, "آی‌پی درگاه با آی‌پی ثبت شده مغایرت دارد." },
        { -11, "مرچنت کد نامعتبر است." },
        { -12, "تلاش بیش از حد در بازه زمانی کوتاه." },
        { -22, "شناسه پرداخت نامعتبر یا منقضی شده است." },
        { -50, "مبلغ پرداخت معتبر نیست." },
        { -51, "پرداخت یافت نشد." },
        { -52, "خطای غیرمنتظره در درگاه." },
        { -53, "شناسه پرداخت با تراکنش مطابقت ندارد." },
        { -54, "درخواست مورد نظر آرشیو شده است." },
        { -1, "اطلاعات ارسال شده ناقص است." }
    };

    public static string GetMessage(int code)
    {
        if (ErrorMessages.TryGetValue(code, out var message))
        {
            return message;
        }
        return "خطای ناشناخته در درگاه پرداخت.";
    }
}