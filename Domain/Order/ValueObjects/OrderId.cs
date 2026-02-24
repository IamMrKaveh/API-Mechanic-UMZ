namespace Domain.Order.ValueObjects;

public sealed class OrderId : ValueObject
{
    public int Value { get; }

    private OrderId(int value) => Value = value;

    public static OrderId Create(int value)
    {
        if (value <= 0)
            throw new DomainException("شناسه سفارش باید عدد مثبت باشد.");
        return new OrderId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator int(OrderId orderId) => orderId.Value;
}