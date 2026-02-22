namespace Application.Cart.Features.Shared;

public record CartDetailDto
{
    public int Id { get; init; }
    public int? UserId { get; init; }
    public string? GuestToken { get; init; }
    public List<CartItemDetailDto> Items { get; init; } = new();
    public decimal TotalPrice { get; init; }
    public int TotalItems { get; init; }
    public List<CartPriceChangeDto> PriceChanges { get; init; } = new();
}

public record CartItemDto
{
    public int Id { get; init; }
    public int CartId { get; init; }
    public int VariantId { get; init; }
    public int Quantity { get; init; }
    public string? ProductName { get; init; }
    public decimal SellingPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public string? ProductIcon { get; init; }
    public Dictionary<string, AttributeValueDto>? Attributes { get; init; }
    public string? RowVersion { get; init; }
}

public record CartItemDetailDto
{
    public int VariantId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? VariantSku { get; init; }
    public string? ProductImage { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public int Stock { get; init; }
    public bool IsUnlimited { get; init; }
    public bool IsAvailable { get; init; }
    public Dictionary<string, string>? Attributes { get; init; }
}

public record CartPriceChangeDto(
    int VariantId,
    string ProductName,
    decimal OldPrice,
    decimal NewPrice
);

public record CartSummaryDto
{
    public int ItemCount { get; init; }
    public int TotalQuantity { get; init; }
    public decimal TotalPrice { get; init; }
}

public record CartCheckoutValidationDto
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<CartPriceChangeDto> PriceChanges { get; init; } = new();
    public List<CartStockIssueDto> StockIssues { get; init; } = new();
}

public record CartStockIssueDto(
    int VariantId,
    string ProductName,
    int RequestedQuantity,
    int AvailableStock
);