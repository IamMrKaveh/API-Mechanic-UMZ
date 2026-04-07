namespace Domain.Attribute.ValueObjects;

public sealed record AttributeValueId
{
    public Guid Value { get; }

    private AttributeValueId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("AttributeValueId cannot be empty.", nameof(value));

        Value = value;
    }

    public static AttributeValueId NewId() => new(Guid.NewGuid());

    public static AttributeValueId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}