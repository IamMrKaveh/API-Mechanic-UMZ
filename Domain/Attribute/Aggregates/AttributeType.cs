using Domain.Attribute.Entities;
using Domain.Attribute.Events;
using Domain.Attribute.ValueObjects;

namespace Domain.Attribute.Aggregates;

public sealed class AttributeType : AggregateRoot<AttributeTypeId>, IAuditable, IActivatable
{
    private readonly List<AttributeValue> _values = [];

    public string Name { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<AttributeValue> Values => _values.AsReadOnly();

    private AttributeType()
    { }

    private AttributeType(AttributeTypeId id, string name, string displayName, int sortOrder, bool isActive) : base(id)
    {
        Name = name;
        DisplayName = displayName;
        SortOrder = sortOrder;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    public static AttributeType Create(string name, string displayName, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام ویژگی الزامی است.");

        var id = AttributeTypeId.NewId();
        var attributeType = new AttributeType(id, name.Trim(), displayName?.Trim() ?? name.Trim(), sortOrder, isActive);
        attributeType.RaiseDomainEvent(new AttributeTypeCreatedEvent(id, name.Trim(), displayName?.Trim() ?? name.Trim(), sortOrder));
        return attributeType;
    }

    public void Update(string name, string displayName, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام ویژگی الزامی است.");

        Name = name.Trim();
        DisplayName = displayName?.Trim() ?? name.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public AttributeValue AddValue(string value, string displayValue, string? hexCode = null, int sortOrder = 0)
    {
        if (_values.Any(v => v.Value.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"مقدار '{value}' قبلاً برای این ویژگی وجود دارد.");

        var attributeValue = AttributeValue.Create(this, value, displayValue, hexCode, sortOrder);
        _values.Add(attributeValue);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new AttributeValueAddedEvent(Id, attributeValue.Id, value.Trim(), displayValue?.Trim() ?? value.Trim()));
        return attributeValue;
    }

    public void UpdateValue(AttributeValueId valueId, string value, string displayValue, string? hexCode, int sortOrder, bool isActive)
    {
        var attrValue = _values.FirstOrDefault(v => v.Id == valueId) ?? throw new DomainException("مقدار ویژگی یافت نشد.");
        if (!attrValue.Value.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            if (_values.Any(v => v.Id != valueId && v.Value.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new DomainException($"مقدار '{value}' قبلاً وجود دارد.");
        }

        attrValue.Update(value, displayValue, hexCode, sortOrder, isActive);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveValue(AttributeValueId valueId)
    {
        var attrValue = _values.FirstOrDefault(v => v.Id == valueId) ?? throw new DomainException("مقدار ویژگی یافت نشد.");
        _values.Remove(attrValue);
        UpdatedAt = DateTime.UtcNow;
    }
}