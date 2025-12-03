namespace Application.DTOs;

public enum CartOperationResult
{
    Success,
    NotFound,
    OutOfStock,
    Error,
    ConcurrencyConflict
}

public record AddToCartDto(
    [Required] int VariantId,
    [Range(1, 1000)] int Quantity,
    byte[]? VariantRowVersion,
    byte[]? CartItemRowVersion
);

public record UpdateCartItemDto(
    [Range(0, 1000)] int Quantity,
    byte[]? RowVersion
);

public record CartItemDto(
    int Id,
    int VariantId,
    string ProductName,
    decimal SellingPrice,
    decimal SavedPrice,
    int Quantity,
    string? ProductIcon,
    decimal TotalPrice,
    string? RowVersion,
    Dictionary<string, AttributeValueDto> Attributes,
    bool HasPriceChanged
);

public record CartDto(
    int Id,
    int? UserId,
    string? GuestToken,
    ICollection<CartItemDto> CartItems,
    int TotalItems,
    decimal TotalPrice,
    List<CartPriceChangeDto> PriceChanges
);

public class CartPriceChangeDto
{
    public int VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public int Quantity { get; set; }
    public decimal PriceDifference => NewPrice - OldPrice;
    public decimal TotalDifference => PriceDifference * Quantity;
}

public class CheckoutFromCartResultDto
{
    public int OrderId { get; set; }
    public string? PaymentUrl { get; set; }
    public string? Authority { get; set; }
    public string? Error { get; set; }
}