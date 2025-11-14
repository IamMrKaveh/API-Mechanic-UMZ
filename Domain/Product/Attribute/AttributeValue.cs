namespace Domain.Product.Attribute;

public class AttributeValue : IAuditable
{
    public int Id { get; set; }

    public int AttributeTypeId { get; set; }
    public AttributeType AttributeType { get; set; } = null!;

    public required string Value { get; set; }

    public required string DisplayValue { get; set; }

    public string? HexCode { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProductVariantAttribute> VariantAttributes { get; set; } = [];
}