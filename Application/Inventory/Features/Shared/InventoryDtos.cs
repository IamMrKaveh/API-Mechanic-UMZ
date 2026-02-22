namespace Application.Inventory.Features.Shared;

public record InventoryTransactionDto
{
    public int Id { get; init; }
    public int VariantId { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public int QuantityChange { get; init; }
    public int StockBefore { get; init; }
    public int StockAfter => StockBefore + QuantityChange;
    public string? Notes { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsReversed { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ProductName { get; init; }
    public string? Sku { get; init; }
    public string? VariantSku { get; init; }
    public string? UserName { get; init; }
}

public record InventoryStatusDto
{
    public int VariantId { get; init; }
    public int StockQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int AvailableStock { get; init; }
    public bool IsInStock { get; init; }
    public bool IsUnlimited { get; init; }
}

public record LowStockItemDto
{
    public int VariantId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public int StockQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int AvailableStock { get; init; }
    public int LowStockThreshold { get; init; }
}

public record OutOfStockItemDto
{
    public int VariantId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public int StockQuantity { get; init; }
    public int ReservedQuantity { get; init; }
}

public record InventoryStatisticsDto
{
    public int TotalVariants { get; init; }
    public int InStockCount { get; init; }
    public int LowStockCount { get; init; }
    public int OutOfStockCount { get; init; }
    public int UnlimitedCount { get; init; }
    public decimal TotalInventoryValue { get; init; }
    public decimal TotalSellingValue { get; init; }
}

public record VariantStockStatusDto
{
    public int VariantId { get; init; }
    public int StockQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int AvailableStock { get; init; }
    public bool IsInStock { get; init; }
    public bool IsUnlimited { get; init; }
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
    public List<BulkAdjustItemResultDto> Results { get; init; } = new();
}

public record ReconcileResultDto
{
    public int VariantId { get; init; }
    public int FinalStock { get; init; }
    public int Difference { get; init; }
    public bool HasDiscrepancy { get; init; }
    public string? Message { get; init; }
}

public class BulkStockInRequest
{
    public List<BulkStockInItemRequest> Items { get; init; } = [];
    public string? SupplierReference { get; init; }
}

public class BulkStockInItemRequest
{
    public int VariantId { get; init; }
    public int Quantity { get; init; }
    public string? Notes { get; init; }
}

public class ApproveReturnRequest
{
    public string? Reason { get; init; }
}

public record BulkAdjustItemDto
{
    public int VariantId { get; init; }
    public int QuantityChange { get; init; }
    public string Notes { get; init; } = string.Empty;
}

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
    public List<BulkStockInItemResultDto> Results { get; init; } = new();
}