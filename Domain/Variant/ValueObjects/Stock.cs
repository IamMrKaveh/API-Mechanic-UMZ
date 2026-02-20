namespace Domain.Variant.ValueObjects;

public sealed class Stock : ValueObject
{
    public int Quantity { get; }
    public int Reserved { get; }
    public bool IsUnlimited { get; }

    private Stock(int quantity, int reserved, bool isUnlimited)
    {
        Quantity = quantity;
        Reserved = reserved;
        IsUnlimited = isUnlimited;
    }

    public static Stock Create(int quantity, bool isUnlimited = false)
    {
        if (!isUnlimited && quantity < 0)
            throw new DomainException("موجودی نمی‌تواند منفی باشد.");

        return new Stock(quantity, 0, isUnlimited);
    }

    public static Stock Unlimited() => new(0, 0, true);

    public static Stock Zero() => new(0, 0, false);

    public int Available => IsUnlimited ? int.MaxValue : Math.Max(0, Quantity - Reserved);

    public bool CanFulfill(int quantity) => IsUnlimited || Available >= quantity;

    public bool IsInStock => IsUnlimited || Available > 0;

    public bool IsOutOfStock => !IsUnlimited && Available <= 0;

    public Stock Add(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("مقدار افزایش باید بزرگتر از صفر باشد.");

        if (IsUnlimited)
            return this;

        return new Stock(Quantity + quantity, Reserved, IsUnlimited);
    }

    public Stock Reduce(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("مقدار کاهش باید بزرگتر از صفر باشد.");
        if (IsUnlimited)
            return this;

        if (Available < quantity)
            throw new DomainException($"موجودی کافی نیست. موجودی قابل فروش: {Available}، درخواستی: {quantity}");

        return new Stock(Quantity - quantity, Reserved, IsUnlimited);
    }

    public Stock Reserve(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("مقدار رزرو باید بزرگتر از صفر باشد.");

        if (IsUnlimited)
            return this;

        if (Available < quantity)
            throw new DomainException($"موجودی کافی برای رزرو نیست. موجودی قابل رزرو: {Available}");

        return new Stock(Quantity, Reserved + quantity, IsUnlimited);
    }

    public Stock Release(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("مقدار آزادسازی باید بزرگتر از صفر باشد.");

        if (IsUnlimited)
            return this;

        var releaseAmount = Math.Min(quantity, Reserved);
        return new Stock(Quantity, Reserved - releaseAmount, IsUnlimited);
    }

    public Stock ConfirmReservation(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("مقدار تأیید باید بزرگتر از صفر باشد.");

        if (IsUnlimited)
            return this;

        if (Reserved < quantity)
            throw new DomainException($"موجودی رزرو شده کافی نیست. رزرو شده: {Reserved}، درخواستی: {quantity}");

        return new Stock(Quantity - quantity, Reserved - quantity, IsUnlimited);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Quantity;
        yield return Reserved;
        yield return IsUnlimited;
    }

    public override string ToString()
    {
        if (IsUnlimited)
            return "نامحدود";

        return Reserved > 0
            ? $"{Quantity} (رزرو شده: {Reserved})"
            : Quantity.ToString();
    }
}