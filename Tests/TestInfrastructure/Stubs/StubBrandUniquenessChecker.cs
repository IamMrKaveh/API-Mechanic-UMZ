using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Tests.TestInfrastructure.Stubs;

public sealed class StubBrandUniquenessChecker : IBrandUniquenessChecker
{
    private bool _isUnique = true;

    public int CallCount { get; private set; }
    public BrandName? LastName { get; private set; }
    public BrandSlug? LastSlug { get; private set; }
    public CategoryId? LastCategoryId { get; private set; }
    public BrandId? LastExcludeId { get; private set; }

    public StubBrandUniquenessChecker WithIsUnique(bool value)
    {
        _isUnique = value;
        return this;
    }

    public Task<bool> IsUniqueAsync(
        BrandName name,
        BrandSlug slug,
        CategoryId categoryId,
        BrandId? excludeId,
        CancellationToken ct)
    {
        CallCount++;
        LastName = name;
        LastSlug = slug;
        LastCategoryId = categoryId;
        LastExcludeId = excludeId;
        return Task.FromResult(_isUnique);
    }
}
