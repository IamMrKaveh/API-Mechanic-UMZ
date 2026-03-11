namespace Application.Cart.Features.Shared;

public record CartDetailDto
{
    public int Id { get; init; }
    public int? UserId { get; init; }
    public string? GuestToken { get; init; }
    public List<CartItemDetailDto> Items { get; init; } = [];
    public decimal TotalPrice { get; init; }
    public int TotalItems { get; init; }
    public List<CartPriceChangeDto> PriceChanges { get; init; } = [];
}

public record CartItemDto(
    int Id,
    int CartId,
    int VariantId,
    int Quantity,
    string? ProductName,
    decimal SellingPrice,
    decimal TotalPrice,
    string? ProductIcon,
    Dictionary<string, AttributeValueDto>? Attributes,
    string? RowVersion
);

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
    public List<string> Errors { get; init; } = [];
    public List<CartPriceChangeDto> PriceChanges { get; init; } = [];
    public List<CartStockIssueDto> StockIssues { get; init; } = [];
}

public record CartStockIssueDto
{
    public int VariantId { get; init; }
    public string ProductName { get; init; }
    public int RequestedQuantity { get; init; }
    public int AvailableStock { get; init; }
}