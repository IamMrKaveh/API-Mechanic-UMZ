using Domain.Inventory.Entities;
using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class StockLedgerEntryBuilder
{
    private VariantId _variantId = VariantId.NewId();
    private int _quantity = 10;
    private int _balanceAfter = 10;
    private decimal _unitCost;
    private string? _referenceNumber = "REF-001";
    private string? _note;
    private WarehouseId? _warehouseId;
    private UserId? _userId;
    private OrderItemId? _orderItemId;

    public StockLedgerEntryBuilder WithVariantId(VariantId id)
    { _variantId = id; return this; }

    public StockLedgerEntryBuilder WithQuantity(int q)
    { _quantity = q; return this; }

    public StockLedgerEntryBuilder WithBalanceAfter(int b)
    { _balanceAfter = b; return this; }

    public StockLedgerEntryBuilder WithUnitCost(decimal c)
    { _unitCost = c; return this; }

    public StockLedgerEntryBuilder WithReferenceNumber(string? r)
    { _referenceNumber = r; return this; }

    public StockLedgerEntryBuilder WithNote(string? n)
    { _note = n; return this; }

    public StockLedgerEntryBuilder WithWarehouseId(WarehouseId? w)
    { _warehouseId = w; return this; }

    public StockLedgerEntryBuilder WithUserId(UserId? u)
    { _userId = u; return this; }

    public StockLedgerEntryBuilder WithOrderItemId(OrderItemId? o)
    { _orderItemId = o; return this; }

    public StockLedgerEntry BuildStockIn() =>
        StockLedgerEntry.StockIn(_variantId, _quantity, _balanceAfter, _unitCost,
            _referenceNumber, _note, _warehouseId, _userId);

    public StockLedgerEntry BuildReserve() =>
        StockLedgerEntry.Reserve(_variantId, _quantity, _balanceAfter,
            _referenceNumber ?? string.Empty, correlationId: null,
            warehouseId: _warehouseId, userId: _userId, orderItemId: _orderItemId);

    public StockLedgerEntry BuildReleaseReservation() =>
        StockLedgerEntry.ReleaseReservation(_variantId, _quantity, _balanceAfter,
            _referenceNumber ?? string.Empty, _note, _warehouseId);

    public StockLedgerEntry BuildCommitReservation() =>
        StockLedgerEntry.CommitReservation(_variantId, _quantity, _balanceAfter,
            _referenceNumber ?? string.Empty, _orderItemId, _warehouseId);

    public StockLedgerEntry BuildAdjustment() =>
        StockLedgerEntry.Adjustment(_variantId, _quantity, _balanceAfter,
            _note ?? "adjustment", _userId, _warehouseId);
}
