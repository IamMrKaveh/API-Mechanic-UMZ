namespace Domain.Order.ValueObjects;

public sealed record OrderItemId : IStronglyTypedId
{
    public Guid Value { get; }

    private OrderItemId(Guid value) => Value = value;

    public static OrderItemId NewId() => new(Guid.NewGuid());

    public static OrderItemId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("OrderItemId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(OrderItemId id) => id.Value;
}