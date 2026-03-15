namespace Domain.Product.Specifications;

public class InStockProductSpecification : Specification<Inventory.Aggregates.Inventory>
{
    public override Expression<Func<Inventory.Aggregates.Inventory, bool>> ToExpression()
    {
        return i => i.IsUnlimited || i.AvailableQuantity > 0;
    }
}