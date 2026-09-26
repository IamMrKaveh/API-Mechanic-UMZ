namespace Domain.Attribute.ValueObjects;

public sealed record AttributeTypeId : StronglyTypedId<AttributeTypeId>
{
    private AttributeTypeId(Guid value) : base(value) { }

    public static implicit operator Guid(AttributeTypeId id) => id.Value;

    public override string ToString() => Value.ToString();
}
