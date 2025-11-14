namespace Domain.Product.Attribute;

public class ProductVariantAttribute
{
    public int Id { get; set; }

    public int VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    public int AttributeValueId { get; set; }
    public AttributeValue AttributeValue { get; set; } = null!;
}