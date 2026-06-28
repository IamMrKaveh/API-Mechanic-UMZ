namespace Domain.Payment.ValueObjects;

public sealed record PaymentMethodId : IStronglyTypedId
{
    public Guid Value { get; }

    private PaymentMethodId(Guid value) => Value = value;

    public static PaymentMethodId NewId() => new(Guid.NewGuid());

    public static PaymentMethodId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("PaymentMethodId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(PaymentMethodId id) => id.Value;
}