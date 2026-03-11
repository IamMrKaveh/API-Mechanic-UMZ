namespace Domain.Payment.ValueObjects;

public sealed record PaymentTransactionId(Guid Value)
{
    public static PaymentTransactionId NewId() => new(Guid.NewGuid());
    public static PaymentTransactionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}