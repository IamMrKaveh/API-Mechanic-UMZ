using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.Aggregates;
using Domain.Product.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class ProductBuilder
{
    private static readonly Faker Faker = new();

    private ProductName _name = new ProductNameBuilder().Build();
    private ProductSlug _slug = new ProductSlugBuilder().Build();
    private string _description = Faker.Commerce.ProductDescription();
    private BrandId _brandId = BrandId.NewId();
    private CategoryId _categoryId = CategoryId.NewId();

    public ProductBuilder WithName(ProductName name)
    {
        _name = name;
        return this;
    }

    public ProductBuilder WithName(string value)
    {
        _name = ProductName.Create(value);
        return this;
    }

    public ProductBuilder WithSlug(ProductSlug slug)
    {
        _slug = slug;
        return this;
    }

    public ProductBuilder WithSlug(string value)
    {
        _slug = ProductSlug.Create(value);
        return this;
    }

    public ProductBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public ProductBuilder WithBrandId(BrandId brandId)
    {
        _brandId = brandId;
        return this;
    }

    public ProductBuilder WithCategoryId(CategoryId categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public Product Build() =>
        Product.Create(_name, _slug, _description, _brandId, _categoryId);
}
