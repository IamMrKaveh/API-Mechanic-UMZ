namespace Application.Cart.Features.Shared;

// ====== DTOs مشترک بین Command و Query ======

/// <summary>
/// DTO کامل سبد خرید برای نمایش در صفحه سبد
/// </summary>
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

/// <summary>
/// DTO آیتم سبد خرید با اطلاعات کامل محصول
/// </summary>
public class CartItemDto
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public string? ProductName { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? ProductIcon { get; set; }
    public Dictionary<string, AttributeValueDto>? Attributes { get; set; }
    public string? RowVersion { get; set; }
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

/// <summary>
/// تغییر قیمت شناسایی شده
/// </summary>
public record CartPriceChangeDto(
    int VariantId,
    string ProductName,
    decimal OldPrice,
    decimal NewPrice
);

/// <summary>
/// خلاصه سبد خرید برای هدر سایت
/// </summary>
public record CartSummaryDto
{
    public int ItemCount { get; init; }
    public int TotalQuantity { get; init; }
    public decimal TotalPrice { get; init; }
}

/// <summary>
/// نتیجه اعتبارسنجی سبد برای پرداخت
/// </summary>
public record CartCheckoutValidationDto
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<CartPriceChangeDto> PriceChanges { get; init; } = new();
    public List<CartStockIssueDto> StockIssues { get; init; } = new();
}

/// <summary>
/// مشکل موجودی آیتم سبد
/// </summary>
public record CartStockIssueDto(
    int VariantId,
    string ProductName,
    int RequestedQuantity,
    int AvailableStock
);