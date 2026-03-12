namespace Domain.Variant.Specifications;

public class LowStockVariantSpecification : Specification<Inventory.Aggregates.Inventory>
{
    private readonly int _threshold;

    public LowStockVariantSpecification(int threshold = 5)
    {
        if (threshold < 0)
            throw new DomainException("آستانه کم‌موجودی نمی‌تواند منفی باشد.");

        _threshold = threshold;
    }

    public override Expression<Func<Inventory.Aggregates.Inventory, bool>> ToExpression()
    {
        return i => !i.IsUnlimited
                    && i.AvailableQuantity > 0
                    && i.AvailableQuantity <= _threshold;
    }
}