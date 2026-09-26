namespace Domain.Order.ValueObjects;

public sealed record OrderStatusId : StronglyTypedId<OrderStatusId>
{
    private OrderStatusId(Guid value) : base(value) { }

    public static implicit operator Guid(OrderStatusId id) => id.Value;

    public override string ToString() => Value.ToString();
}
