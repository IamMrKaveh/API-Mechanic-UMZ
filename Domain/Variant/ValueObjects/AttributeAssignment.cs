using Domain.Attribute.ValueObjects;

namespace Domain.Variant.ValueObjects;

public sealed record AttributeAssignment(
    AttributeId AttributeId,
    AttributeValueId ValueId,
    string DisplayValue);