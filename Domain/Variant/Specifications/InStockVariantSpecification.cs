namespace Domain.Variant.Specifications;

public class InStockVariantSpecification : Specification<ProductVariant>
{
    public override Expression<Func<ProductVariant, bool>> ToExpression()
    {
        return v => v.IsUnlimited || (v.StockQuantity - v.ReservedQuantity) > 0;
    }
}