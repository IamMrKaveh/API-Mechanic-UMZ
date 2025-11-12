namespace DataAccessLayer.Models.Product.Attribute;

[Index(nameof(AttributeTypeId), nameof(Value))]
public class TAttributeValue : IAuditable
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(AttributeTypeId))]
    public virtual TAttributeType AttributeType { get; set; } = null!;
    public int AttributeTypeId { get; set; }

    [Required(ErrorMessage = "مقدار ویژگی الزامی است")]
    [StringLength(100)]
    public string Value { get; set; } = string.Empty;

    [StringLength(100)]
    public string DisplayValue { get; set; } = string.Empty;

    [StringLength(7)]
    public string? HexCode { get; set; }

    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<TProductVariantAttribute> VariantAttributes { get; set; } = new List<TProductVariantAttribute>();
}