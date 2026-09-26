namespace Domain.Payment.ValueObjects;

public sealed record PaymentMethodId : StronglyTypedId<PaymentMethodId>
{
    private PaymentMethodId(Guid value) : base(value) { }

    public static implicit operator Guid(PaymentMethodId id) => id.Value;

    public override string ToString() => Value.ToString();
}
