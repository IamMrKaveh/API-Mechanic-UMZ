using Domain.Attribute.ValueObjects;

namespace Domain.Attribute.Interfaces;

public interface IAttributeTypeUniquenessChecker
{
    bool IsUnique(
        string name,
        AttributeTypeId? excludeId = null);
}