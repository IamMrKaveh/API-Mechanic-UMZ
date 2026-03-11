namespace Domain.Attribute.ValueObjects;

public sealed record AttributeValueId(Guid Value)
{
    public static AttributeValueId NewId() => new(Guid.NewGuid());
    public static AttributeValueId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}