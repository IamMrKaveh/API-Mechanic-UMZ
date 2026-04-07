namespace Domain.Cart.ValueObjects;

public sealed record CartId
{
    public Guid Value { get; }

    private CartId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("CartId cannot be empty.", nameof(value));

        Value = value;
    }

    public static CartId NewId() => new(Guid.NewGuid());

    public static CartId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}