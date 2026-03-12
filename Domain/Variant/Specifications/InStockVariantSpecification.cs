namespace Domain.Variant.Specifications;

public class InStockVariantSpecification : Specification<Inventory.Aggregates.Inventory>
{
    public override Expression<Func<Inventory.Aggregates.Inventory, bool>> ToExpression()
    {
        return i => i.IsUnlimited || i.AvailableQuantity > 0;
    }
}