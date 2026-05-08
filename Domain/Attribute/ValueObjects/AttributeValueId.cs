namespace Domain.Attribute.ValueObjects;

public sealed record AttributeValueId : IStronglyTypedId
{
    public Guid Value { get; }

    private AttributeValueId(Guid value) => Value = value;

    public static AttributeValueId NewId() => new(Guid.NewGuid());

    public static AttributeValueId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("AttributeValueId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(AttributeValueId id) => id.Value;
}