namespace Domain.Order.ValueObjects;

public sealed record OrderId : IStronglyTypedId
{
    public Guid Value { get; }

    private OrderId(Guid value) => Value = value;

    public static OrderId NewId() => new(Guid.NewGuid());

    public static OrderId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("OrderId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(OrderId id) => id.Value;
}