using Domain.Attribute.Aggregates;
using Domain.Attribute.ValueObjects;
using Domain.Variant.Entities;

namespace Domain.Attribute.Entities;

public sealed class AttributeValue : Entity<AttributeValueId>, IAuditable, IActivatable, ISoftDeletable
{
    public AttributeTypeId AttributeTypeId { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public string DisplayValue { get; private set; } = null!;
    public string? HexCode { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public AttributeType AttributeType { get; private set; } = null!;
    public ICollection<ProductVariantAttribute> VariantAttributes { get; private set; } = new List<ProductVariantAttribute>();

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    private AttributeValue()
    { }

    private AttributeValue(
        AttributeValueId id,
        AttributeType attributeType,
        string value,
        string displayValue,
        string? hexCode,
        int sortOrder) : base(id)
    {
        AttributeType = attributeType;
        AttributeTypeId = attributeType.Id;
        Value = value;
        DisplayValue = displayValue;
        HexCode = hexCode;
        SortOrder = sortOrder;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    internal static AttributeValue Create(AttributeType attributeType, string value, string displayValue, string? hexCode, int sortOrder)
    {
        Guard.Against.Null(attributeType, nameof(attributeType));
        Guard.Against.NullOrWhiteSpace(value, nameof(value));
        Guard.Against.Negative(sortOrder, nameof(sortOrder));

        var trimmedValue = value.Trim();
        var trimmedDisplay = string.IsNullOrWhiteSpace(displayValue) ? trimmedValue : displayValue.Trim();

        return new AttributeValue(
            AttributeValueId.NewId(),
            attributeType,
            trimmedValue,
            trimmedDisplay,
            hexCode?.Trim(),
            sortOrder);
    }

    public void Update(string value, string displayValue, string? hexCode, int sortOrder, bool isActive)
    {
        Guard.Against.NullOrWhiteSpace(value, nameof(value));
        Guard.Against.Negative(sortOrder, nameof(sortOrder));

        Value = value.Trim();
        DisplayValue = string.IsNullOrWhiteSpace(displayValue) ? Value : displayValue.Trim();
        HexCode = hexCode?.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}