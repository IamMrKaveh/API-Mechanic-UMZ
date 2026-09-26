namespace Domain.Payment.ValueObjects;

public sealed record PaymentTransactionId : StronglyTypedId<PaymentTransactionId>
{
    private PaymentTransactionId(Guid value) : base(value) { }

    public static implicit operator Guid(PaymentTransactionId id) => id.Value;

    public override string ToString() => Value.ToString();
}
