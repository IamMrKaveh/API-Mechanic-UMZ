namespace Domain.Attribute;

/// <summary>
/// مقادیر ویژگی (مثل قرمز، آبی، ایکس لارج) - فرزند AttributeType
/// </summary>
public class AttributeValue : BaseEntity, IAuditable, ISoftDeletable, IActivatable
{
    public int AttributeTypeId { get; private set; }
    public string Value { get; private set; } = null!;
    public string DisplayValue { get; private set; } = null!;
    public string? HexCode { get; private set; } 
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    
    public AttributeType AttributeType { get; private set; } = null!;

    public ICollection<ProductVariantAttribute> VariantAttributes { get; private set; } = new List<ProductVariantAttribute>();

    private AttributeValue()
    { }

    internal static AttributeValue Create(AttributeType attributeType, string value, string displayValue, string? hexCode, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException("مقدار الزامی است.");

        return new AttributeValue
        {
            AttributeType = attributeType,
            AttributeTypeId = attributeType.Id,
            Value = value.Trim(),
            DisplayValue = displayValue?.Trim() ?? value.Trim(),
            HexCode = hexCode?.Trim(),
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void Update(string value, string displayValue, string? hexCode, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException("مقدار الزامی است.");

        Value = value.Trim();
        DisplayValue = displayValue?.Trim() ?? value.Trim();
        HexCode = hexCode?.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
    }
}