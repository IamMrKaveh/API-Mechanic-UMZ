using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class BatchReservationItemBuilder
{
    private VariantId _variantId = VariantId.NewId();
    private StockQuantity _quantity = StockQuantity.Create(1);

    public BatchReservationItemBuilder WithVariantId(VariantId id)
    { _variantId = id; return this; }

    public BatchReservationItemBuilder WithQuantity(int q)
    { _quantity = StockQuantity.Create(q); return this; }

    public BatchReservationItemBuilder WithQuantity(StockQuantity q)
    { _quantity = q; return this; }

    public BatchReservationItem Build() => new(_variantId, _quantity);
}
