namespace Application.Inventory.Features.Shared;

public class InventoryTransactionDto
{
    public int Id { get; set; }
    public int VariantId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter => StockBefore + QuantityChange;
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

public class InventoryStatusDto
{
    public int VariantId { get; set; }
    public int StockQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableStock { get; set; }
    public bool IsInStock { get; set; }
    public bool IsUnlimited { get; set; }
}

public class LowStockItemDto
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

public class OutOfStockItemDto
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int StockQuantity { get; set; }
    public int ReservedQuantity { get; set; }
}

public class InventoryStatisticsDto
{
    public int TotalVariants { get; set; }
    public int InStockCount { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int UnlimitedCount { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public decimal TotalSellingValue { get; set; }
}