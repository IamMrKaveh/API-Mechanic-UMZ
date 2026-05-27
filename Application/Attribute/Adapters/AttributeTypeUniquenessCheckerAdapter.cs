using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Application.Attribute.Adapters;

public sealed class AttributeTypeUniquenessCheckerAdapter(IAttributeRepository repository)
    : IAttributeTypeUniquenessChecker
{
    public async Task<bool> IsUniqueAsync(string name, AttributeTypeId? excludeId = null, CancellationToken ct = default)
    {
        return !await repository.AttributeTypeExistsAsync(name, excludeId, ct);
    }
}