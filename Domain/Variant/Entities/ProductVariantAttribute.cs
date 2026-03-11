namespace Domain.Variant.Entities;

public sealed class ProductVariantAttribute : Entity<ProductVariantAttributeId>
{
    private ProductVariantAttribute()
    { }

    public ProductVariantId VariantId { get; private set; } = default!;
    public AttributeId AttributeId { get; private set; } = default!;
    public AttributeValueId ValueId { get; private set; } = default!;
    public string DisplayValue { get; private set; } = default!;

    internal static ProductVariantAttribute Create(
        ProductVariantId variantId,
        AttributeId attributeId,
        AttributeValueId valueId,
        string displayValue)
    {
        return new ProductVariantAttribute
        {
            Id = ProductVariantAttributeId.NewId(),
            VariantId = variantId,
            AttributeId = attributeId,
            ValueId = valueId,
            DisplayValue = displayValue
        };
    }
}