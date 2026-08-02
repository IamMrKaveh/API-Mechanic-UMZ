using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Tests.TestInfrastructure.Stubs;

public sealed class StubAttributeTypeUniquenessChecker : IAttributeTypeUniquenessChecker
{
    private bool _isUnique = true;

    public int CallCount { get; private set; }
    public string? LastName { get; private set; }
    public AttributeTypeId? LastExcludeId { get; private set; }

    public StubAttributeTypeUniquenessChecker WithIsUnique(bool value)
    {
        _isUnique = value;
        return this;
    }

    public Task<bool> IsUniqueAsync(
        string name,
        AttributeTypeId? excludeId = null,
        CancellationToken ct = default)
    {
        CallCount++;
        LastName = name;
        LastExcludeId = excludeId;
        return Task.FromResult(_isUnique);
    }
}
