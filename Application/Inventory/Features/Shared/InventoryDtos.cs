namespace Application.Inventory.Features.Shared;

public record InventoryTransactionDto
{
    public int Id { get; set; }
    public int VariantId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
    public string? Notes { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsReversed { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ProductName { get; set; }
    public string? Sku { get; set; }
    public string? VariantSku { get; set; }
    public string? UserName { get; set; }
}

public record InventoryStatusDto
{
    public int VariantId { get; set; }
    public int StockQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableStock { get; set; }
    public bool IsInStock { get; set; }
    public bool IsUnlimited { get; set; }
}

public record LowStockItemDto
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int StockQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableStock { get; set; }
    public int LowStockThreshold { get; set; }
}

public record OutOfStockItemDto
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int StockQuantity { get; set; }
    public int ReservedQuantity { get; set; }
}

public record InventoryStatisticsDto(
    int TotalVariants,
    int InStockCount,
    int LowStockCount,
    int OutOfStockCount,
    int UnlimitedCount,
    decimal TotalInventoryValue,
    decimal TotalSellingValue
);

public record VariantStockStatusDto
{
    public int VariantId { get; set; }
    public int StockQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableStock { get; set; }
    public bool IsInStock { get; set; }
    public bool IsUnlimited { get; set; }
}

public record BulkAdjustItemResultDto
{
    public int VariantId { get; init; }
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public int NewStock { get; init; }
}

public record BulkAdjustResultDto
{
    public int TotalRequested { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public List<BulkAdjustItemResultDto> Results { get; init; } = [];
}

public record ReconcileResultDto
{
    public int VariantId { get; init; }
    public int FinalStock { get; init; }
    public int Difference { get; init; }
    public bool HasDiscrepancy { get; init; }
    public string? Message { get; init; }
}

public record BulkStockInRequest(
    List<BulkStockInItemRequest> Items,
    string? SupplierReference
);

public record BulkStockInItemRequest(
    int VariantId,
    int Quantity,
    string? Notes
);

public record ApproveReturnRequest(
    string? Reason
);

public record BulkAdjustItemDto(
    int VariantId,
    int QuantityChange,
    string Notes
);

public record BulkStockInItemDto
{
    public int VariantId { get; init; }
    public int Quantity { get; init; }
    public string? Notes { get; init; }
}

public record BulkStockInItemResultDto
{
    public int VariantId { get; init; }
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public int NewStock { get; init; }
}

public record BulkStockInResultDto
{
    public int TotalRequested { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public List<BulkStockInItemResultDto> Results { get; init; } = [];
}

public record WarehouseStockDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
}

public record StockLedgerEntryDto(
    int Id,
    int VariantId,
    int? WarehouseId,
    string EventType,
    int QuantityDelta,
    int BalanceAfter,
    decimal UnitCost,
    string? ReferenceNumber,
    string? Note,
    string? Source,
    DateTime CreatedAt
);