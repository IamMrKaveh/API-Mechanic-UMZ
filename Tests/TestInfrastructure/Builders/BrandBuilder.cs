using Domain.Brand.Aggregates;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Tests.TestInfrastructure.Stubs;

namespace Tests.TestInfrastructure.Builders;

public sealed class BrandBuilder
{
    private BrandName _name = new BrandNameBuilder().Build();
    private BrandSlug _slug = new BrandSlugBuilder().Build();
    private CategoryId _categoryId = CategoryId.NewId();
    private string? _description;
    private string? _logoPath;
    private IBrandUniquenessChecker _uniquenessChecker = new StubBrandUniquenessChecker();

    public BrandBuilder WithName(BrandName name)
    {
        _name = name;
        return this;
    }

    public BrandBuilder WithName(string value)
    {
        _name = BrandName.Create(value);
        return this;
    }

    public BrandBuilder WithSlug(BrandSlug slug)
    {
        _slug = slug;
        return this;
    }

    public BrandBuilder WithSlug(string value)
    {
        _slug = BrandSlug.Create(value);
        return this;
    }

    public BrandBuilder WithCategoryId(CategoryId categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public BrandBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public BrandBuilder WithLogoPath(string? logoPath)
    {
        _logoPath = logoPath;
        return this;
    }

    public BrandBuilder WithUniquenessChecker(IBrandUniquenessChecker checker)
    {
        _uniquenessChecker = checker;
        return this;
    }

    public Task<Brand> BuildAsync(CancellationToken ct = default) =>
        Brand.Create(_name, _slug, _categoryId, _uniquenessChecker, _description, _logoPath, ct);
}
