namespace Domain.Product.Specifications;

public class InStockProductSpecification : Specification<Inventory>
{
    public override Expression<Func<Inventory, bool>> ToExpression()
    {
        return i => i.IsUnlimited || i.AvailableQuantity > 0;
    }
}