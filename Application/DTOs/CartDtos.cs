namespace Application.DTOs;

public record AddToCartDto(
    [Required] int VariantId,
    [Range(1, 1000)] int Quantity,
    byte[]? RowVersion
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