namespace Domain.Variant;

/// <summary>
/// موجودیت واسط برای ارتباط چند-به-چند بین واریانت و مقادیر ویژگی
/// </summary>
public class ProductVariantAttribute : BaseEntity
{
    public int VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    public int AttributeValueId { get; set; }
    public AttributeValue AttributeValue { get; set; } = null!;
}