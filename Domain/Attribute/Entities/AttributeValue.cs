using Domain.Attribute.Aggregates;
using Domain.Attribute.ValueObjects;

namespace Domain.Attribute.Entities;

public sealed class AttributeValue : Entity<AttributeValueId>, IAuditable, IActivatable, ISoftDeletable
{
    public string Value { get; private set; } = null!;
    public string DisplayValue { get; private set; } = null!;
    public string? HexCode { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public AttributeTypeId AttributeTypeId { get; private set; } = null!;
    public AttributeType AttributeType { get; private set; } = null!;

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
        int sortOrder,
        DateTime now) : base(id)
    {
        AttributeType = attributeType;
        AttributeTypeId = attributeType.Id;
        Value = value;
        DisplayValue = displayValue;
        HexCode = hexCode;
        SortOrder = sortOrder;
        CreatedAt = now;
        IsActive = true;
    }

    internal static AttributeValue Create(AttributeType attributeType, string value, string displayValue, string? hexCode, int sortOrder, DateTime now)
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
            sortOrder,
            now);
    }

    public void Update(string value, string displayValue, string? hexCode, int sortOrder, bool isActive, DateTime now)
    {
        Guard.Against.NullOrWhiteSpace(value, nameof(value));
        Guard.Against.Negative(sortOrder, nameof(sortOrder));

        Value = value.Trim();
        DisplayValue = string.IsNullOrWhiteSpace(displayValue) ? Value : displayValue.Trim();
        HexCode = hexCode?.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = now;
    }
}