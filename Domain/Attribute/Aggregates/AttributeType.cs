using Domain.Attribute.Entities;
using Domain.Attribute.Events;
using Domain.Attribute.Exceptions;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Domain.Attribute.Aggregates;

public sealed class AttributeType : AggregateRoot<AttributeTypeId>, IAuditable, IActivatable, ISoftDeletable
{
    private readonly List<AttributeValue> _values = [];
    public IReadOnlyCollection<AttributeValue> Values => _values.AsReadOnly();

    public string Name { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

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

    public static AttributeType Create(
        string name,
        string displayName,
        int sortOrder,
        bool isActive,
        IAttributeTypeUniquenessChecker uniquenessChecker)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.Null(uniquenessChecker, nameof(uniquenessChecker));
        Guard.Against.Negative(sortOrder, nameof(sortOrder));

        var trimmedName = name.Trim();
        var trimmedDisplayName = string.IsNullOrWhiteSpace(displayName) ? trimmedName : displayName.Trim();

        if (!uniquenessChecker.IsUnique(trimmedName))
            throw new DuplicateAttributeException(trimmedName);

        var id = AttributeTypeId.NewId();
        var attributeType = new AttributeType(id, trimmedName, trimmedDisplayName, sortOrder, isActive);

        attributeType.RaiseDomainEvent(new AttributeTypeCreatedEvent(id, trimmedName, trimmedDisplayName, sortOrder));
        return attributeType;
    }

    public void Update(
        string name,
        string displayName,
        int sortOrder,
        bool isActive,
        IAttributeTypeUniquenessChecker uniquenessChecker)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.Null(uniquenessChecker, nameof(uniquenessChecker));
        Guard.Against.Negative(sortOrder, nameof(sortOrder));

        var trimmedName = name.Trim();
        var trimmedDisplayName = string.IsNullOrWhiteSpace(displayName) ? trimmedName : displayName.Trim();

        if (!Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase) && !uniquenessChecker.IsUnique(trimmedName, Id))
            throw new DuplicateAttributeException(trimmedName);

        Name = trimmedName;
        DisplayName = trimmedDisplayName;
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public AttributeValue AddValue(string value, string displayValue, string? hexCode = null, int sortOrder = 0)
    {
        Guard.Against.NullOrWhiteSpace(value, nameof(value));
        Guard.Against.Negative(sortOrder, nameof(sortOrder));

        var trimmedValue = value.Trim();

        if (_values.Any(v => v.Value.Equals(trimmedValue, StringComparison.OrdinalIgnoreCase)))
            throw new DuplicateAttributeException(trimmedValue);

        var attributeValue = AttributeValue.Create(this, trimmedValue, displayValue, hexCode, sortOrder);
        _values.Add(attributeValue);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new AttributeValueAddedEvent(Id, attributeValue.Id, trimmedValue, attributeValue.DisplayValue));
        return attributeValue;
    }

    public void UpdateValue(
        AttributeValueId valueId,
        AttributeValue value,
        string displayValue,
        string? hexCode,
        int sortOrder,
        bool isActive)
    {
        Guard.Against.NullOrWhiteSpace(value.Value, nameof(value));
        Guard.Against.Negative(sortOrder, nameof(sortOrder));

        var attrValue = _values.FirstOrDefault(v => v.Id == valueId)
            ?? throw new AttributeValueNotFoundException(valueId);

        var trimmedValue = value.Value.Trim();

        if (!attrValue.Value.Equals(trimmedValue, StringComparison.OrdinalIgnoreCase))
        {
            if (_values.Any(v => v.Id != valueId && v.Value.Equals(trimmedValue, StringComparison.OrdinalIgnoreCase)))
                throw new DuplicateAttributeException(trimmedValue);
        }

        attrValue.Update(trimmedValue, displayValue, hexCode, sortOrder, isActive);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveValue(AttributeValueId valueId)
    {
        var attrValue = _values.FirstOrDefault(v => v.Id == valueId)
            ?? throw new AttributeValueNotFoundException(valueId);

        _values.Remove(attrValue);
        UpdatedAt = DateTime.UtcNow;
    }
}