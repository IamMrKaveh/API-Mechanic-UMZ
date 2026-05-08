namespace Domain.Cart.ValueObjects;

public sealed record CartItemId : IStronglyTypedId
{
    public Guid Value { get; }

    private CartItemId(Guid value) => Value = value;

    public static CartItemId NewId() => new(Guid.NewGuid());

    public static CartItemId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("CartItemId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(CartItemId id) => id.Value;
}