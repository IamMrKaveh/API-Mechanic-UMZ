namespace Domain.Order.ValueObjects;

public sealed record OrderItemId : StronglyTypedId<OrderItemId>
{
    private OrderItemId(Guid value) : base(value) { }

    public static implicit operator Guid(OrderItemId id) => id.Value;

    public override string ToString() => Value.ToString();
}
