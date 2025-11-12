namespace DataAccessLayer.Models.Product.Attribute;

public class TProductVariantAttribute
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(VariantId))]
    public virtual TProductVariant Variant { get; set; } = null!;
    public int VariantId { get; set; }

    [ForeignKey(nameof(AttributeValueId))]
    public virtual TAttributeValue AttributeValue { get; set; } = null!;
    public int AttributeValueId { get; set; }
}
