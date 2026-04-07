namespace Domain.Cart.ValueObjects;

public sealed record CartItemId
{
    public Guid Value { get; }

    private CartItemId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("CartItemId cannot be empty.", nameof(value));

        Value = value;
    }

    public static CartItemId NewId() => new(Guid.NewGuid());

    public static CartItemId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}