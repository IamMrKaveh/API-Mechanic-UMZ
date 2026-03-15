using Domain.Attribute.ValueObjects;

namespace Domain.Variant.ValueObjects;

public sealed record AttributeAssignment(
    AttributeTypeId AttributeId,
    AttributeValueId ValueId,
    string DisplayValue)
{
    public static AttributeAssignment Create(
        AttributeTypeId attributeId,
        AttributeValueId valueId,
        string displayValue)
    {
        Guard.Against.Null(attributeId, nameof(attributeId));
        Guard.Against.Null(valueId, nameof(valueId));
        Guard.Against.NullOrWhiteSpace(displayValue, nameof(displayValue));

        return new AttributeAssignment(attributeId, valueId, displayValue.Trim());
    }
}