namespace Domain.Payment.ValueObjects;

public sealed record PaymentTransactionId : IStronglyTypedId
{
    public Guid Value { get; }

    private PaymentTransactionId(Guid value) => Value = value;

    public static PaymentTransactionId NewId() => new(Guid.NewGuid());

    public static PaymentTransactionId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("PaymentTransactionId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(PaymentTransactionId id) => id.Value;
}