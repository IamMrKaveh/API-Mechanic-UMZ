using Application.Attribute.Features.Shared;

namespace Application.Cart.Features.Shared;

public record CartDetailDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? GuestToken { get; init; }
    public bool IsCheckedOut { get; init; }
    public List<CartItemDetailDto> Items { get; init; } = [];
    public decimal TotalPrice { get; init; }
    public int TotalItems { get; init; }
    public List<CartPriceChangeDto> PriceChanges { get; init; } = [];
}

public record CartItemDto
{
    public Guid Id { get; init; }
    public Guid CartId { get; init; }
    public Guid VariantId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal OriginalPrice { get; init; }
    public int Quantity { get; init; }
    public decimal TotalPrice { get; init; }
    public DateTime AddedAt { get; init; }
    public string? ProductIcon { get; init; }
    public Dictionary<string, AttributeValueDto>? Attributes { get; init; }
}

public record CartItemDetailDto
{
    public Guid VariantId { get; init; }
    public Guid ProductId { get; init; }
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
    Guid VariantId,
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
    public Guid VariantId { get; init; }
    public string ProductName { get; init; } = null!;
    public int RequestedQuantity { get; init; }
    public int AvailableStock { get; init; }
}

public sealed record SyncCartPricesResult
{
    public bool HasChanges { get; init; }
    public List<CartPriceChangeDto> PriceChanges { get; init; } = [];
    public List<Guid> RemovedVariantIds { get; init; } = [];
}

public sealed record AddToCartDto(
    Guid VariantId,
    int Quantity
);

public sealed record UpdateCartItemDto(
    int Quantity
);