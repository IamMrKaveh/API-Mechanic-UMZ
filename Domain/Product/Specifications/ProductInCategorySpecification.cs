namespace Domain.Product.Specifications;

public class ProductInCategorySpecification : Specification<Product>
{
    private readonly int _categoryId;

    public ProductInCategorySpecification(int categoryId)
    {
        _categoryId = categoryId;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.Brand != null
        && p.Brand.CategoryId == _categoryId;
    }
}