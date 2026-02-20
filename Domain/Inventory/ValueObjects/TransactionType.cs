namespace Domain.Inventory.ValueObjects;

public sealed class TransactionType : ValueObject
{
    public string Value { get; }
    public string DisplayName { get; }
    public bool IsIncrease { get; }

    private TransactionType(string value, string displayName, bool isIncrease)
    {
        Value = value;
        DisplayName = displayName;
        IsIncrease = isIncrease;
    }

    public static TransactionType StockIn => new("StockIn", "ورود موجودی", true);
    public static TransactionType StockOut => new("StockOut", "خروج موجودی", false);
    public static TransactionType Sale => new("Sale", "فروش", false);
    public static TransactionType Reservation => new("Reservation", "رزرو", false);
    public static TransactionType ReservationRollback => new("ReservationRollback", "لغو رزرو", true);
    public static TransactionType Adjustment => new("Adjustment", "تنظیم موجودی", true);
    public static TransactionType Return => new("Return", "برگشت از فروش", true);
    public static TransactionType Reversal => new("Reversal", "برگشت تراکنش", true);
    public static TransactionType Transfer => new("Transfer", "انتقال", false);
    public static TransactionType Damage => new("Damage", "ضایعات", false);
    public static TransactionType Correction => new("Correction", "اصلاح", true);

    /// <summary>
    /// تأیید رزرو پس از پرداخت موفق - کسر Reserved و OnHand به‌صورت توأم
    /// تفکیک از Sale برای ردیابی دقیق‌تر جریان مالی انبار
    /// </summary>
    public static TransactionType Commit => new("Commit", "تأیید رزرو (پرداخت موفق)", false);

    public static TransactionType FromString(string value)
    {
        return value?.ToUpperInvariant() switch
        {
            "STOCKIN" => StockIn,
            "STOCKOUT" => StockOut,
            "SALE" => Sale,
            "RESERVATION" => Reservation,
            "RESERVATIONROLLBACK" => ReservationRollback,
            "ADJUSTMENT" => Adjustment,
            "RETURN" => Return,
            "REVERSAL" => Reversal,
            "TRANSFER" => Transfer,
            "DAMAGE" => Damage,
            "CORRECTION" => Correction,
            "COMMIT" => Commit,
            _ => throw new DomainException($"نوع تراکنش '{value}' نامعتبر است.")
        };
    }

    public static IEnumerable<TransactionType> GetAll()
    {
        yield return StockIn;
        yield return StockOut;
        yield return Sale;
        yield return Reservation;
        yield return ReservationRollback;
        yield return Adjustment;
        yield return Return;
        yield return Reversal;
        yield return Transfer;
        yield return Damage;
        yield return Correction;
        yield return Commit;
    }

    public static IEnumerable<TransactionType> GetIncreaseTypes()
    {
        return GetAll().Where(t => t.IsIncrease);
    }

    public static IEnumerable<TransactionType> GetDecreaseTypes()
    {
        return GetAll().Where(t => !t.IsIncrease);
    }

    public bool RequiresOrderItem()
    {
        return this == Sale || this == Reservation || this == Return || this == Commit;
    }

    public bool RequiresUserApproval()
    {
        return this == Adjustment || this == Damage || this == Correction;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => DisplayName;

    public static implicit operator string(TransactionType type) => type.Value;
}