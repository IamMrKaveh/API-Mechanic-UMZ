namespace Domain.Cart.ValueObjects;

public sealed record CartId : IStronglyTypedId
{
    public Guid Value { get; }

    private CartId(Guid value) => Value = value;

    public static CartId NewId() => new(Guid.NewGuid());

    public static CartId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("CartId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(CartId id) => id.Value;
}