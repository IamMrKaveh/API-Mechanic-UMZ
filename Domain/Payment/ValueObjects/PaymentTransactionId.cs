namespace Domain.Payment.ValueObjects;

public sealed record PaymentTransactionId
{
    public Guid Value { get; }

    private PaymentTransactionId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PaymentTransactionId cannot be empty.", nameof(value));

        Value = value;
    }

    public static PaymentTransactionId NewId() => new(Guid.NewGuid());

    public static PaymentTransactionId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}