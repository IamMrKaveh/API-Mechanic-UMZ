namespace Domain.Attribute.ValueObjects;

public sealed record AttributeTypeId(Guid Value)
{
    public static AttributeTypeId NewId() => new(Guid.NewGuid());
    public static AttributeTypeId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}