namespace Domain.Payment.ValueObjects;

public sealed class PaymentMethodFee : ValueObject
{
    public Money Amount { get; }
    public decimal Percentage { get; }

    public PaymentMethodFee()
    { }

    private PaymentMethodFee(Money amount, decimal percentage)
    {
        Amount = amount;
        Percentage = percentage;
    }

    public static PaymentMethodFee None() => new(Money.Zero(), 0m);

    public static PaymentMethodFee Create(decimal fixedAmount, decimal percentage)
    {
        if (fixedAmount < 0)
            throw new DomainException("کارمزد ثابت نمی‌تواند منفی باشد.");
        if (percentage < 0)
            throw new DomainException("درصد کارمزد نمی‌تواند منفی باشد.");
        if (percentage > 100)
            throw new DomainException("درصد کارمزد نمی‌تواند بیش از ۱۰۰ باشد.");
        return new PaymentMethodFee(Money.FromDecimal(fixedAmount), percentage);
    }

    public Money CalculateFor(Money orderTotal)
    {
        if (orderTotal is null) return Money.Zero();
        var percentagePart = Math.Round(orderTotal.Amount * Percentage / 100m, 0);
        var total = Amount.Amount + percentagePart;
        return Money.FromDecimal(total);
    }

    public bool IsZero => Amount.Amount == 0m && Percentage == 0m;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount?.Amount ?? 0m;
        yield return Percentage;
    }
}