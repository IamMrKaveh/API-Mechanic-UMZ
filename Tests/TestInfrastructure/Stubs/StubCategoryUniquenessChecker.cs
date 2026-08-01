using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Tests.TestInfrastructure.Stubs;

public sealed class StubCategoryUniquenessChecker : ICategoryUniquenessChecker
{
    private bool _isUnique = true;

    public int CallCount { get; private set; }
    public CategoryName? LastName { get; private set; }
    public CategorySlug? LastSlug { get; private set; }
    public CategoryId? LastExcludeId { get; private set; }

    public StubCategoryUniquenessChecker WithIsUnique(bool value)
    {
        _isUnique = value;
        return this;
    }

    public Task<bool> IsUniqueAsync(
        CategoryName name,
        CategorySlug slug,
        CategoryId? excludeId,
        CancellationToken ct)
    {
        CallCount++;
        LastName = name;
        LastSlug = slug;
        LastExcludeId = excludeId;
        return Task.FromResult(_isUnique);
    }
}
