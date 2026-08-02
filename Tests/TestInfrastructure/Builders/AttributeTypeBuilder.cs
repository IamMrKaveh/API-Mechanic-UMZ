using Domain.Attribute.Aggregates;
using Domain.Attribute.Interfaces;
using Tests.TestInfrastructure.Stubs;

namespace Tests.TestInfrastructure.Builders;

public sealed class AttributeTypeBuilder
{
    private static readonly Faker Faker = new();

    private string _name = Faker.Commerce.ProductAdjective();
    private string _displayName = Faker.Commerce.ProductAdjective();
    private int _sortOrder = Faker.Random.Int(0, 100);
    private bool _isActive = true;
    private IAttributeTypeUniquenessChecker _uniquenessChecker = new StubAttributeTypeUniquenessChecker();

    public AttributeTypeBuilder WithName(string value)
    {
        _name = value;
        return this;
    }

    public AttributeTypeBuilder WithDisplayName(string value)
    {
        _displayName = value;
        return this;
    }

    public AttributeTypeBuilder WithSortOrder(int value)
    {
        _sortOrder = value;
        return this;
    }

    public AttributeTypeBuilder WithIsActive(bool value)
    {
        _isActive = value;
        return this;
    }

    public AttributeTypeBuilder WithUniquenessChecker(IAttributeTypeUniquenessChecker checker)
    {
        _uniquenessChecker = checker;
        return this;
    }

    public Task<AttributeType> BuildAsync(CancellationToken ct = default) =>
        AttributeType.Create(_name, _displayName, _sortOrder, _isActive, _uniquenessChecker, ct);
}
