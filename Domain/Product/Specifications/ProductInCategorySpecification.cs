namespace Domain.Product.Specifications;

public class ProductInCategorySpecification : Specification<Aggregates.Product>
{
    private readonly CategoryId _categoryId;

    public ProductInCategorySpecification(CategoryId categoryId)
    {
        _categoryId = categoryId;
    }

    public override Expression<Func<Aggregates.Product, bool>> ToExpression()
    {
        return p => p.IsActive && p.CategoryId == _categoryId;
    }
}