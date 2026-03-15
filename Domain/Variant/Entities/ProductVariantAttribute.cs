using Domain.Attribute.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Entities;

public sealed class ProductVariantAttribute : Entity<ProductVariantAttributeId>
{
    private ProductVariantAttribute()
    { }

    public ProductVariantId VariantId { get; private set; } = default!;
    public AttributeTypeId AttributeTypeId { get; private set; } = default!;
    public AttributeValueId ValueId { get; private set; } = default!;
    public string DisplayValue { get; private set; } = default!;

    internal static ProductVariantAttribute Create(
        ProductVariantId variantId,
        AttributeTypeId attributeId,
        AttributeValueId valueId,
        string displayValue)
    {
        return new ProductVariantAttribute
        {
            Id = ProductVariantAttributeId.NewId(),
            VariantId = variantId,
            AttributeTypeId = attributeId,
            ValueId = valueId,
            DisplayValue = displayValue
        };
    }
}