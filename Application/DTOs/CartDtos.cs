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
    int Quantity,
    string? ProductIcon,
    decimal TotalPrice,
    byte[]? RowVersion,
    Dictionary<string, AttributeValueDto> Attributes
);

public record CartDto(
    int Id,
    int? UserId,
    string? GuestToken,
    ICollection<CartItemDto> CartItems,
    int TotalItems,
    decimal TotalPrice
);

public class CheckoutFromCartResultDto
{
    public int OrderId { get; set; }
    public string? PaymentUrl { get; set; }
    public string? Authority { get; set; }
    public string? Error { get; set; }
}