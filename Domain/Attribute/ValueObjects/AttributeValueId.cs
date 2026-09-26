namespace Domain.Attribute.ValueObjects;

public sealed record AttributeValueId : StronglyTypedId<AttributeValueId>
{
    private AttributeValueId(Guid value) : base(value) { }

    public static implicit operator Guid(AttributeValueId id) => id.Value;

    public override string ToString() => Value.ToString();
}
