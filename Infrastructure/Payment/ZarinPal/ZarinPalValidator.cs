namespace Infrastructure.Payment.ZarinPal;

public class ZarinPalValidator
{
    public static void ValidateRequest(decimal amount, string description, string callbackUrl)
    {
        if (amount < 1000)
            throw new ArgumentException("مبلغ تراکنش باید حداقل 1000 ریال باشد.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("توضیحات تراکنش الزامی است.");

        if (description.Length > 500)
            throw new ArgumentException("توضیحات تراکنش نباید بیشتر از 500 کاراکتر باشد.");

        if (string.IsNullOrWhiteSpace(callbackUrl) || !Uri.TryCreate(callbackUrl, UriKind.Absolute, out _))
            throw new ArgumentException("آدرس بازگشت نامعتبر است.");
    }
}