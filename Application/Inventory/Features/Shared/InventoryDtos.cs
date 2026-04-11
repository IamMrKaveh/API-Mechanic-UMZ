using Application.Inventory.Features.Commands.BulkAdjustStock;
using Application.Inventory.Features.Commands.BulkStockIn;

namespace Application.Inventory.Features.Shared;

public record InventoryTransactionDto
{
    public Guid Id { get; init; }
    public Guid VariantId { get; init; }
    public string? ProductName { get; init; }
    public string? VariantSku { get; init; }
    public string? Sku { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public int QuantityChange { get; init; }
    public int BalanceAfter { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? Notes { get; init; }
    public string? UserName { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record InventoryDto
{
    public Guid Id { get; init; }
    public Guid VariantId { get; init; }
    public int StockQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public bool IsUnlimited { get; init; }
    public bool IsInStock { get; init; }
    public bool IsLowStock { get; init; }
    public int LowStockThreshold { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record StockLedgerEntryDto
{
    public Guid Id { get; init; }
    public Guid VariantId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public int QuantityDelta { get; init; }
    public int BalanceAfter { get; init; }
    public string? Note { get; init; }
    public string? ReferenceNumber { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record VariantAvailabilityDto
{
    public Guid VariantId { get; init; }
    public bool IsAvailable { get; init; }
    public int AvailableQuantity { get; init; }
    public bool IsUnlimited { get; init; }
    public bool IsLowStock { get; init; }
}

public sealed record ReverseInventoryDto(
    Guid VariantId,
    string IdempotencyKey,
    string Reason
);

public sealed record AdjustStockDto(
    Guid VariantId,
    int QuantityChange,
    string Reason
);

public sealed record BulkAdjustStockDto(
    IReadOnlyList<BulkAdjustStockItem> Items,
    string Reason
);

public sealed record RecordDamageDto(
    Guid VariantId,
    int Quantity,
    string Reason
);

public sealed record BulkStockInDto(
    IReadOnlyList<BulkStockInItem> Items,
    string Reason
);

public sealed record BatchAvailabilityDto(
    ICollection<Guid> VariantIds
);

public sealed record ApproveReturnDto(
    string? Reason = null
);