namespace Domain.Product.Specifications;

public class InStockProductSpecification : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.Stats.TotalStock > 0 || p.Variants.Any(v => v.IsUnlimited);
    }
}