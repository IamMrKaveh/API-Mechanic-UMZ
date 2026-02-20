namespace Domain.Variant.Specifications;

public class LowStockVariantSpecification : Specification<ProductVariant>
{
    private readonly int _threshold;

    public LowStockVariantSpecification(int threshold = 5)
    {
        _threshold = threshold;
    }

    public override Expression<Func<ProductVariant, bool>> ToExpression()
    {
        return v => !v.IsUnlimited
                    && !v.IsDeleted
                    && v.IsActive
                    && (v.StockQuantity - v.ReservedQuantity) <= _threshold;
    }
}