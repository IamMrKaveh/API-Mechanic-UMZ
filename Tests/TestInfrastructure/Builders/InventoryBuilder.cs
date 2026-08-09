using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using Inv = Domain.Inventory.Aggregates.Inventory;

namespace Tests.TestInfrastructure.Builders;

public sealed class InventoryBuilder
{
    private VariantId _variantId = VariantId.NewId();
    private int _initialStock;
    private bool _isUnlimited;
    private int _lowStockThreshold = 5;
    private UserId? _createdBy;

    public InventoryBuilder WithVariantId(VariantId variantId)
    {
        _variantId = variantId;
        return this;
    }

    public InventoryBuilder WithInitialStock(int initialStock)
    {
        _initialStock = initialStock;
        return this;
    }

    public InventoryBuilder AsUnlimited()
    {
        _isUnlimited = true;
        return this;
    }

    public InventoryBuilder WithLowStockThreshold(int threshold)
    {
        _lowStockThreshold = threshold;
        return this;
    }

    public InventoryBuilder WithCreatedBy(UserId userId)
    {
        _createdBy = userId;
        return this;
    }

    public Inv Build() =>
        Inv.Create(_variantId, _initialStock, _isUnlimited, _lowStockThreshold, _createdBy);
}
