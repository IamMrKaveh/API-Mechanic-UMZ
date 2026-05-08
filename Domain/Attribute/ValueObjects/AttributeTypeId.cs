namespace Domain.Attribute.ValueObjects;

public sealed record AttributeTypeId : IStronglyTypedId
{
    public Guid Value { get; }

    private AttributeTypeId(Guid value) => Value = value;

    public static AttributeTypeId NewId() => new(Guid.NewGuid());

    public static AttributeTypeId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("AttributeTypeId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(AttributeTypeId id) => id.Value;
}