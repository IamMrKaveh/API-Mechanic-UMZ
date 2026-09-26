namespace Domain.Order.ValueObjects;

public sealed record OrderId : StronglyTypedId<OrderId>
{
    private OrderId(Guid value) : base(value) { }

    public static implicit operator Guid(OrderId id) => id.Value;

    public override string ToString() => Value.ToString();
}
