using Domain.Attribute.ValueObjects;

namespace Domain.Attribute.Interfaces;

public interface IAttributeTypeUniquenessChecker
{
    Task<bool> IsUniqueAsync(
        string name,
        AttributeTypeId? excludeId = null,
        CancellationToken ct = default);
}