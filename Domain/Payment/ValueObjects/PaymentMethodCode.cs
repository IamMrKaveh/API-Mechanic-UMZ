using System.Text.RegularExpressions;

namespace Domain.Payment.ValueObjects;

public sealed partial class PaymentMethodCode : ValueObject
{
    public string Value { get; }
    public const int MaxLength = 50;

    public const string ZarinpalSandbox = "zarinpal-sandbox";
    public const string Zarinpal = "zarinpal";
    public const string CashOnDelivery = "cash-on-delivery";
    public const string Wallet = "wallet";

    private static readonly Regex AllowedPattern = AllowedPatternRegex();

    private PaymentMethodCode(string value) => Value = value;

    public static PaymentMethodCode Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("کد روش پرداخت الزامی است.");
        var normalized = code.Trim().ToLowerInvariant();
        if (normalized.Length > MaxLength)
            throw new DomainException($"کد روش پرداخت نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");
        if (!AllowedPattern.IsMatch(normalized))
            throw new DomainException("کد روش پرداخت تنها می‌تواند شامل حروف کوچک انگلیسی، اعداد و خط تیره باشد.");
        return new PaymentMethodCode(normalized);
    }

    public bool IsWallet => Value == Wallet;
    public bool IsCashOnDelivery => Value == CashOnDelivery;
    public bool IsOnlineGateway => Value == Zarinpal || Value == ZarinpalSandbox;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PaymentMethodCode code) => code.Value;

    [GeneratedRegex("^[a-z0-9-]+$", RegexOptions.Compiled)]
    private static partial Regex AllowedPatternRegex();
}