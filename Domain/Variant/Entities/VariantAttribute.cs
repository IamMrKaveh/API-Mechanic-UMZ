using Domain.Attribute.Aggregates;
using Domain.Attribute.Entities;
using Domain.Attribute.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Entities;

public sealed class VariantAttribute : Entity<VariantAttributeId>
{
    private VariantAttribute()
    { }

    public Aggregates.ProductVariant Variant { get; private set; } = default!;
    public VariantId VariantId { get; private set; } = default!;
    public AttributeType AttributeType { get; private set; } = default!;
    public AttributeTypeId AttributeTypeId { get; private set; } = default!;
    public AttributeValue Value { get; private set; } = default!;
    public AttributeValueId ValueId { get; private set; } = default!;
    public string DisplayValue { get; private set; } = default!;

    internal static VariantAttribute Create(
        VariantId variantId,
        AttributeTypeId attributeId,
        AttributeValueId valueId,
        string displayValue)
    {
        return new VariantAttribute
        {
            Id = VariantAttributeId.NewId(),
            VariantId = variantId,
            AttributeTypeId = attributeId,
            ValueId = valueId,
            DisplayValue = displayValue
        };
    }
}