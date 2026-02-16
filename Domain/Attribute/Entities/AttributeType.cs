using Domain.Common.Base;
using Domain.Common.Interfaces;
using Domain.Common.Exceptions;

namespace Domain.Attribute.Entities;

/// <summary>
/// Aggregate Root برای ویژگی‌ها (مثل رنگ، سایز، جنس)
/// </summary>
public class AttributeType : AggregateRoot, IAuditable, ISoftDeletable, IActivatable
{
    public string Name { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Audit & Soft Delete
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Navigation (Child Entities)
    private readonly List<AttributeValue> _values = new();

    public IReadOnlyCollection<AttributeValue> AttributeValues => _values.AsReadOnly();

    // Alias for compatibility
    public IReadOnlyCollection<AttributeValue> Values => _values.AsReadOnly();

    private AttributeType()
    { }

    // ============================================================
    // Factory
    // ============================================================
    public static AttributeType Create(string name, string displayName, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("نام ویژگی الزامی است.");

        return new AttributeType
        {
            Name = name.Trim(),
            DisplayName = displayName?.Trim() ?? name.Trim(),
            SortOrder = sortOrder,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ============================================================
    // Behavior
    // ============================================================
    public void Update(string name, string displayName, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("نام ویژگی الزامی است.");
        if (string.IsNullOrWhiteSpace(displayName)) throw new DomainException("نام نمایشی الزامی است.");

        Name = name.Trim();
        DisplayName = displayName.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public AttributeValue AddValue(string value, string displayValue, string? hexCode = null, int sortOrder = 0)
    {
        if (_values.Any(v => v.Value.Equals(value, StringComparison.OrdinalIgnoreCase) && !v.IsDeleted))
            throw new DomainException($"مقدار '{value}' قبلاً برای این ویژگی وجود دارد.");

        var attributeValue = AttributeValue.Create(this, value, displayValue, hexCode, sortOrder);
        _values.Add(attributeValue);
        UpdatedAt = DateTime.UtcNow;
        return attributeValue;
    }

    public void UpdateValue(int valueId, string value, string displayValue, string? hexCode, int sortOrder, bool isActive)
    {
        var attrValue = _values.FirstOrDefault(v => v.Id == valueId);
        if (attrValue == null) throw new DomainException("مقدار ویژگی یافت نشد.");

        attrValue.Update(value, displayValue, hexCode, sortOrder, isActive);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveValue(int valueId, int? deletedBy = null)
    {
        var attrValue = _values.FirstOrDefault(v => v.Id == valueId);
        if (attrValue == null) throw new DomainException("مقدار ویژگی یافت نشد.");

        attrValue.Delete(deletedBy);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(int? deletedBy)
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;

        foreach (var val in _values)
        {
            val.Delete(deletedBy);
        }
    }
}