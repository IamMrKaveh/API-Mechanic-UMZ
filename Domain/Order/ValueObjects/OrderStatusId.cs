namespace Domain.Order.ValueObjects;

public sealed record OrderStatusId : IStronglyTypedId
{
    public Guid Value { get; }

    private OrderStatusId(Guid value) => Value = value;

    public static OrderStatusId NewId() => new(Guid.NewGuid());

    public static OrderStatusId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("OrderStatusId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(OrderStatusId id) => id.Value;
}