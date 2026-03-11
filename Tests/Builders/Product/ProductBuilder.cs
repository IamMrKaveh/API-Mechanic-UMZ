namespace Tests.Builders.Product;

public class ProductBuilder
{
    private string _name = "محصول تست";
    private string _slug = "product-test";
    private int _categoryId = 1;
    private int _brandId = 1;
    private string? _description = "توضیحات محصول تست";

    public ProductBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProductBuilder WithSlug(string slug)
    {
        _slug = slug;
        return this;
    }

    public ProductBuilder WithCategoryId(int categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public ProductBuilder WithBrandId(int brandId)
    {
        _brandId = brandId;
        return this;
    }

    public ProductBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public Domain.Product.Product Build()
    {
        var name = ProductName.Create(_name);
        var slug = Slug.Create(_slug);
        return Domain.Product.Product.Create(name, slug, _categoryId, _brandId, _description);
    }
}