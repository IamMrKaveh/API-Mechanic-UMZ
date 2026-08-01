using Domain.Category.Aggregates;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Tests.TestInfrastructure.Stubs;

namespace Tests.TestInfrastructure.Builders;

public sealed class CategoryBuilder
{
    private CategoryId _id = CategoryId.NewId();
    private CategoryName _name = new CategoryNameBuilder().Build();
    private CategorySlug _slug = new CategorySlugBuilder().Build();
    private string? _description;
    private int _sortOrder = 0;
    private ICategoryUniquenessChecker _uniquenessChecker = new StubCategoryUniquenessChecker();

    public CategoryBuilder WithId(CategoryId id)
    {
        _id = id;
        return this;
    }

    public CategoryBuilder WithName(CategoryName name)
    {
        _name = name;
        return this;
    }

    public CategoryBuilder WithName(string value)
    {
        _name = CategoryName.Create(value);
        return this;
    }

    public CategoryBuilder WithSlug(CategorySlug slug)
    {
        _slug = slug;
        return this;
    }

    public CategoryBuilder WithSlug(string value)
    {
        _slug = CategorySlug.Create(value);
        return this;
    }

    public CategoryBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public CategoryBuilder WithSortOrder(int sortOrder)
    {
        _sortOrder = sortOrder;
        return this;
    }

    public CategoryBuilder WithUniquenessChecker(ICategoryUniquenessChecker checker)
    {
        _uniquenessChecker = checker;
        return this;
    }

    public Task<Category> BuildAsync(CancellationToken ct = default) =>
        Category.Create(_id, _name, _slug, _uniquenessChecker, _description, _sortOrder, ct);
}
