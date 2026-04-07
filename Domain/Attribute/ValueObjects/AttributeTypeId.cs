namespace Domain.Attribute.ValueObjects;

public sealed record AttributeTypeId
{
    public Guid Value { get; }

    private AttributeTypeId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("AttributeTypeId cannot be empty.", nameof(value));

        Value = value;
    }

    public static AttributeTypeId NewId() => new(Guid.NewGuid());

    public static AttributeTypeId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}