using Domain.Common.Base;
using Domain.Common.Interfaces;
using Domain.Common.Exceptions;
using Domain.Variant.Entities;

namespace Domain.Attribute.Entities;

/// <summary>
/// مقادیر ویژگی (مثل قرمز، آبی، ایکس لارج) - فرزند AttributeType
/// </summary>
public class AttributeValue : BaseEntity, IAuditable, ISoftDeletable, IActivatable
{
    public int AttributeTypeId { get; private set; }
    public string Value { get; private set; } = null!;
    public string DisplayValue { get; private set; } = null!;
    public string? HexCode { get; private set; } // برای رنگ‌ها
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Audit & Soft Delete
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Navigation
    public AttributeType AttributeType { get; private set; } = null!;

    // Reverse Navigation for Join Entity
    public ICollection<ProductVariantAttribute> VariantAttributes { get; private set; } = new List<ProductVariantAttribute>();

    private AttributeValue()
    { }

    // Factory Method - Internal (فقط از طریق AttributeType ایجاد شود)
    internal static AttributeValue Create(AttributeType attributeType, string value, string displayValue, string? hexCode, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException("مقدار الزامی است.");
        if (string.IsNullOrWhiteSpace(displayValue)) throw new DomainException("مقدار نمایشی الزامی است.");

        return new AttributeValue
        {
            AttributeType = attributeType,
            AttributeTypeId = attributeType.Id,
            Value = value.Trim(),
            DisplayValue = displayValue.Trim(),
            HexCode = hexCode?.Trim(),
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    internal void Update(string value, string displayValue, string? hexCode, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException("مقدار الزامی است.");

        Value = value.Trim();
        DisplayValue = displayValue.Trim();
        HexCode = hexCode?.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
    }
}