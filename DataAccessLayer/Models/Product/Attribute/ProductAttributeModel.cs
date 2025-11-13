namespace DataAccessLayer.Models.Product.Attribute;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(IsActive), nameof(SortOrder))]
public class TAttributeType : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(50)]
    public required string Name { get; set; }

    [Required, StringLength(50)]
    public required string DisplayName { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int SortOrder { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<TAttributeValue> AttributeValues { get; set; } = [];
}

[Index(nameof(AttributeTypeId), nameof(Value))]
[Index(nameof(IsActive), nameof(SortOrder))]
public class TAttributeValue : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AttributeTypeId { get; set; }
    public TAttributeType AttributeType { get; set; } = null!;

    [Required, StringLength(100)]
    public required string Value { get; set; }

    [Required, StringLength(100)]
    public required string DisplayValue { get; set; }

    [StringLength(7), RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public string? HexCode { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int SortOrder { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<TProductVariantAttribute> VariantAttributes { get; set; } = [];
}

[Index(nameof(VariantId), nameof(AttributeValueId), IsUnique = true)]
[Index(nameof(AttributeValueId))]
public class TProductVariantAttribute
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int VariantId { get; set; }
    public TProductVariant Variant { get; set; } = null!;

    [Required]
    public int AttributeValueId { get; set; }
    public TAttributeValue AttributeValue { get; set; } = null!;
}