namespace Application.DTOs;

public class InventoryTransactionDto
{
    public int Id { get; set; }
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public string? VariantSku { get; set; }
    public string? VariantName { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
    public string? Notes { get; set; }
    public string? ReferenceNumber { get; set; }
    public int? OrderItemId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LowStockItemDto
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int Stock { get; set; }
    public string? CategoryName { get; set; }
    public decimal SellingPrice { get; set; }
    public string? VariantDisplayName { get; set; }
}

public class OutOfStockItemDto
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? CategoryName { get; set; }
    public decimal SellingPrice { get; set; }
    public string? VariantDisplayName { get; set; }
    public DateTime? LastSaleDate { get; set; }
}

public class InventoryStatisticsDto
{
    public int TotalVariants { get; set; }
    public int InStockVariants { get; set; }
    public int LowStockVariants { get; set; }
    public int OutOfStockVariants { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public decimal TotalSellingValue { get; set; }
}