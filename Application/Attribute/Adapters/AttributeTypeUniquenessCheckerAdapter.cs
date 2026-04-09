using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Application.Attribute.Adapters;

public sealed class AttributeTypeUniquenessCheckerAdapter(IAttributeRepository repository)
    : IAttributeTypeUniquenessChecker
{
    private readonly IAttributeRepository _repository = repository;

    public bool IsUnique(string name, AttributeTypeId? excludeId = null)
    {
        return !_repository.AttributeTypeExistsAsync(name, excludeId).GetAwaiter().GetResult();
    }
}