using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Entities;

public sealed class StockLedgerEntry : Entity<StockLedgerEntryId>, IAuditable
{
    public ProductVariantId VariantId { get; private set; } = default!;
    public WarehouseId? WarehouseId { get; private set; }
    public OrderItemId? OrderItemId { get; private set; }
    public UserId? UserId { get; private set; }
    public StockEventType EventType { get; private set; }
    public string EventTypeName => EventType.ToString();
    public int QuantityDelta { get; private set; }
    public int BalanceAfter { get; private set; }
    public decimal UnitCost { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? Note { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt => null;

    private StockLedgerEntry()
    { }

    public static StockLedgerEntry StockIn(
        ProductVariantId variantId,
        int quantity,
        int balanceAfter,
        decimal unitCost,
        string? referenceNumber = null,
        string? note = null,
        WarehouseId? warehouseId = null,
        UserId? userId = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        return Create(variantId, StockEventType.StockIn, quantity, balanceAfter,
            unitCost, referenceNumber, note, warehouseId, userId);
    }

    public static StockLedgerEntry Reserve(
        ProductVariantId variantId,
        int quantity,
        int balanceAfter,
        string referenceNumber,
        string? correlationId = null,
        WarehouseId? warehouseId = null,
        UserId? userId = null,
        OrderItemId? orderItemId = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        var entry = Create(variantId, StockEventType.Reservation, -quantity, balanceAfter,
            0, referenceNumber, null, warehouseId, userId);
        entry.CorrelationId = correlationId;
        entry.OrderItemId = orderItemId;
        return entry;
    }

    public static StockLedgerEntry ReleaseReservation(
        ProductVariantId variantId,
        int quantity,
        int balanceAfter,
        string referenceNumber,
        string? reason = null,
        WarehouseId? warehouseId = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        return Create(variantId, StockEventType.ReservationRelease, quantity, balanceAfter,
            0, referenceNumber, reason, warehouseId);
    }

    public static StockLedgerEntry CommitReservation(
        ProductVariantId variantId,
        int quantity,
        int balanceAfter,
        string referenceNumber,
        OrderItemId? orderItemId = null,
        WarehouseId? warehouseId = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        var entry = Create(variantId, StockEventType.ReservationCommit, -quantity, balanceAfter,
            0, referenceNumber, null, warehouseId);
        entry.OrderItemId = orderItemId;
        return entry;
    }

    public static StockLedgerEntry Adjustment(
        ProductVariantId variantId,
        int delta,
        int balanceAfter,
        string reason,
        UserId? userId = null,
        WarehouseId? warehouseId = null)
    {
        return Create(variantId, StockEventType.Adjustment, delta, balanceAfter,
            0, null, reason, warehouseId, userId);
    }

    private static StockLedgerEntry Create(
        ProductVariantId variantId,
        StockEventType eventType,
        int quantityDelta,
        int balanceAfter,
        decimal unitCost,
        string? referenceNumber,
        string? note,
        WarehouseId? warehouseId = null,
        UserId? userId = null)
    {
        if (balanceAfter < 0)
            throw new DomainException("موجودی پس از این رویداد نمی‌تواند منفی باشد.");

        return new StockLedgerEntry
        {
            Id = StockLedgerEntryId.NewId(),
            VariantId = variantId,
            WarehouseId = warehouseId,
            UserId = userId,
            EventType = eventType,
            QuantityDelta = quantityDelta,
            BalanceAfter = balanceAfter,
            UnitCost = unitCost,
            ReferenceNumber = referenceNumber,
            Note = note,
            IdempotencyKey =
                $"{variantId}:{eventType}:{referenceNumber ?? Guid.NewGuid().ToString("N")}",
            CreatedAt = DateTime.UtcNow
        };
    }
}