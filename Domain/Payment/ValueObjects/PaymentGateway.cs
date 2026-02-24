namespace Domain.Payment.ValueObjects;

public sealed class PaymentGateway : ValueObject
{
    public string Value { get; }
    public string DisplayName { get; }
    public bool IsActive { get; }

    private PaymentGateway(string value, string displayName, bool isActive)
    {
        Value = value;
        DisplayName = displayName;
        IsActive = isActive;
    }

    public static PaymentGateway Zarinpal => new("Zarinpal", "زرین‌پال", true);
    public static PaymentGateway Mellat => new("Mellat", "بانک ملت", true);
    public static PaymentGateway Saman => new("Saman", "بانک سامان", true);
    public static PaymentGateway Parsian => new("Parsian", "بانک پارسیان", true);
    public static PaymentGateway Pasargad => new("Pasargad", "بانک پاسارگاد", true);
    public static PaymentGateway Saderat => new("Saderat", "بانک صادرات", false);
    public static PaymentGateway Wallet => new("Wallet", "کیف پول", true);

    public static PaymentGateway FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("نام درگاه پرداخت الزامی است.");

        return value.ToLowerInvariant() switch
        {
            "zarinpal" => Zarinpal,
            "mellat" => Mellat,
            "saman" => Saman,
            "parsian" => Parsian,
            "pasargad" => Pasargad,
            "saderat" => Saderat,
            "wallet" => Wallet,
            _ => Custom(value)
        };
    }

    public static PaymentGateway Custom(string value, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("نام درگاه پرداخت الزامی است.");

        return new PaymentGateway(
            value.Trim(),
            displayName ?? value.Trim(),
            true);
    }

    public static IEnumerable<PaymentGateway> GetAll()
    {
        yield return Zarinpal;
        yield return Mellat;
        yield return Saman;
        yield return Parsian;
        yield return Pasargad;
        yield return Saderat;
        yield return Wallet;
    }

    public static IEnumerable<PaymentGateway> GetActive()
    {
        return GetAll().Where(g => g.IsActive);
    }

    public bool IsBankGateway() =>
        this == Mellat || this == Saman || this == Parsian || this == Pasargad || this == Saderat;

    public bool IsIPGGateway() =>
        this == Zarinpal;

    public bool IsWallet() =>
        this == Wallet;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => DisplayName;

    public static implicit operator string(PaymentGateway gateway) => gateway.Value;
}