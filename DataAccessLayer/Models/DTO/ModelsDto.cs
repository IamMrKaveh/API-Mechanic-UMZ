namespace DataAccessLayer.Models.DTO;

public record struct ProductSearchDto(
    string? Name = null,
    int? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? InStock = null,
    bool? HasDiscount = null,
    bool? IsUnlimited = null,
    ProductSortOptions SortBy = ProductSortOptions.Newest,
    int Page = 1,
    int PageSize = 10
);

public enum ProductSortOptions
{
    Newest,
    Oldest,
    PriceAsc,
    PriceDesc,
    NameAsc,
    NameDesc,
    DiscountDesc,
    DiscountAsc
}

public record LoginRequestDto(
    [Required, Phone, RegularExpression(@"^09\d{9}$")] string PhoneNumber
);

public record VerifyOtpRequestDto(
    [Required, Phone, RegularExpression(@"^09\d{9}$")] string PhoneNumber,
    [Required, StringLength(4, MinimumLength = 4)] string Code
);

public record AuthResponseDto(
    string Token,
    UserProfileDto User,
    DateTime ExpiresAt,
    string RefreshToken
);

public record UserProfileDto(
    int Id,
    string PhoneNumber,
    string? FirstName,
    string? LastName,
    DateTime? CreatedAt,
    DateTime? DeletedAt,
    bool IsAdmin,
    bool IsActive,
    bool IsDeleted,
    List<UserAddressDto>? Addresses
);

public record UpdateProfileDto(
    [MaxLength(100)] string? FirstName,
    [MaxLength(100)] string? LastName
);

public record RefreshRequestDto([Required] string RefreshToken);

public record UserAddressDto(
    int Id,
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(100)] string ReceiverName,
    [Required, Phone, MaxLength(15)] string PhoneNumber,
    [Required, MaxLength(100)] string Province,
    [Required, MaxLength(100)] string City,
    [Required, MaxLength(500)] string Address,
    [Required, StringLength(10), RegularExpression(@"^\d{10}$")] string PostalCode,
    bool IsDefault
);

public record AddToCartDto(
    [Required, Range(1, int.MaxValue)] int VariantId,
    [Required, Range(1, 1000)] int Quantity = 1,
    byte[]? RowVersion = null
);

public record UpdateCartItemDto(
    [Required, Range(0, 1000)] int Quantity,
    byte[]? RowVersion = null
);

public record CartDto(
    int Id,
    int? UserId,
    string? GuestToken,
    List<CartItemDto> CartItems,
    int TotalItems,
    decimal TotalPrice
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

public record DiscountCodeDto(
    int Id,
    [Required, StringLength(50)] string Code,
    [Range(0, 100)] decimal Percentage,
    decimal? MaxDiscountAmount,
    decimal? MinOrderAmount,
    int? UsageLimit,
    int UsedCount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    int? UserId
);

public record CreateDiscountCodeDto(
    [Required, StringLength(50)] string Code,
    [Range(0.01, 100)] decimal Percentage,
    decimal? MaxDiscountAmount = null,
    decimal? MinOrderAmount = null,
    int? UsageLimit = null,
    DateTime? ExpiresAt = null,
    int? UserId = null
);

public record ApplyDiscountDto(
    [Required, StringLength(50)] string Code,
    [Range(0, double.MaxValue)] decimal OrderTotal
);

public record CreateOrderDto(
    int UserId,
    int UserAddressId,
    int OrderStatusId,
    DateTime? DeliveryDate,
    [Required] int ShippingMethodId,
    string? DiscountCode,
    List<CreateOrderItemDto> OrderItems
);

public record UpdateOrderDto(
    int? UserAddressId,
    int? OrderStatusId,
    DateTime? DeliveryDate,
    int? ShippingMethodId,
    byte[]? RowVersion
);

public record CreateOrderStatusDto([Required] string? Name);

public record UpdateOrderStatusDto(
    [Required] int OrderStatusId,
    string? Name
);

public record CreateOrderItemDto(
    int OrderId,
    int VariantId,
    [Range(1, int.MaxValue)] decimal SellingPrice,
    [Range(1, int.MaxValue)] int Quantity
);

public record UpdateOrderItemDto(
    decimal? SellingPrice,
    int? Quantity,
    byte[]? RowVersion
);

public record CreateOrderFromCartDto(
    [Required] int UserAddressId,
    [Required] int ShippingMethodId,
    string? DiscountCode
);

public record ShippingMethodDto(
    int Id,
    [Required, StringLength(100)] string Name,
    string? Description,
    decimal Cost,
    string? EstimatedDeliveryTime
);

public record PublicOrderViewDto(
    int Id,
    UserAddressDto Address,
    DateTime CreatedAt,
    DateTime? DeliveryDate,
    bool IsPaid,
    decimal TotalAmount,
    decimal ShippingCost,
    decimal DiscountAmount,
    decimal FinalAmount,
    string ShippingMethodName,
    decimal ShippingMethodCost,
    string OrderStatusName,
    List<PublicOrderItemViewDto> OrderItems
);

public record PublicOrderItemProductViewDto(
    int Id,
    string Name,
    string? Icon,
    string? CategoryName,
    string? ColorName,
    string? SizeName
);

public record PublicOrderItemOrderViewDto(
    int Id,
    DateTime CreatedAt
);

public record PublicOrderItemViewDto(
    int Id,
    decimal SellingPrice,
    int Quantity,
    decimal Amount,
    PublicOrderItemProductViewDto? Product,
    PublicOrderItemOrderViewDto? Order
);

public record PublicOrderItemDetailDto(
    int Id,
    decimal SellingPrice,
    int Quantity,
    decimal Amount,
    PublicOrderItemProductViewDto? Product,
    PublicOrderItemOrderViewDto? Order
);

public record AdminOrderItemDetailDto(
    int Id,
    decimal SellingPrice,
    int Quantity,
    decimal Amount,
    PublicOrderItemProductViewDto? Product,
    PublicOrderItemOrderViewDto? Order,
    decimal PurchasePrice,
    decimal Profit
) : PublicOrderItemDetailDto(Id, SellingPrice, Quantity, Amount, Product, Order);

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}


public record ProductStockDto(
    [Range(1, 100000)] int Quantity,
    int? VariantId
);

public record SetDiscountDto(
    [Range(1, double.MaxValue)] decimal OriginalPrice,
    [Range(1, double.MaxValue)] decimal DiscountedPrice
);

public class ProductDto
{
    public int Id { get; set; }
    [Required, StringLength(200)]
    public required string Name { get; set; }
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public List<IFormFile>? Files { get; set; }
    public bool IsActive { get; set; } = true;
    [Required]
    public int CategoryGroupId { get; set; }
    public string? VariantsJson { get; set; }
    public byte[]? RowVersion { get; set; }
}

public record CreateProductVariantDto(
    string? Sku,
    decimal PurchasePrice,
    decimal OriginalPrice,
    decimal SellingPrice,
    int Stock,
    bool IsUnlimited,
    bool IsActive,
    List<int> AttributeValueIds
);

public record AttributeValueDto(
    int Id,
    string Type,
    string TypeDisplay,
    string Value,
    string DisplayValue,
    string? HexCode
);

public record MediaDto(
    int Id,
    string? Url,
    string? AltText,
    bool IsPrimary,
    int SortOrder
);

public record ProductVariantResponseDto(
    int Id,
    string? Sku,
    decimal PurchasePrice,
    decimal OriginalPrice,
    decimal SellingPrice,
    int Stock,
    bool IsUnlimited,
    bool IsInStock,
    double DiscountPercentage,
    List<MediaDto> Images,
    Dictionary<string, AttributeValueDto> Attributes
);

public record PublicProductViewDto(
    int Id,
    string Name,
    string? IconUrl,
    string? Description,
    string? Sku,
    bool IsActive,
    int CategoryGroupId,
    object? CategoryGroup,
    List<ProductVariantResponseDto> Variants,
    List<MediaDto> Images,
    decimal MinPrice,
    decimal MaxPrice,
    int TotalStock,
    bool HasMultipleVariants
);

public record AdminProductViewDto(
    int Id,
    string Name,
    string? IconUrl,
    string? Description,
    string? Sku,
    bool IsActive,
    int CategoryGroupId,
    object? CategoryGroup,
    List<ProductVariantResponseDto> Variants,
    List<MediaDto> Images,
    decimal MinPrice,
    decimal MaxPrice,
    int TotalStock,
    bool HasMultipleVariants,
    byte[]? RowVersion
) : PublicProductViewDto(Id, Name, IconUrl, Description, Sku, IsActive, CategoryGroupId, CategoryGroup, Variants, Images, MinPrice, MaxPrice, TotalStock, HasMultipleVariants);

public record ColorOptionDto(
    int Id,
    [Required, StringLength(50)] string Name,
    [StringLength(10)] string? HexCode = "#FFFFFF"
);

public record SizeOptionDto(
    int Id,
    [Required, StringLength(20)] string Name
);

public class CategoryDto
{
    [Required, StringLength(200)]
    public required string Name { get; set; }
    public IFormFile? IconFile { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class CategoryGroupDto
{
    [Required, StringLength(200)]
    public required string Name { get; set; }
    [Required]
    public int CategoryId { get; set; }
    public IFormFile? IconFile { get; set; }
}

public record ProductReviewDto(
    int Id,
    int ProductId,
    int UserId,
    string? UserName,
    int Rating,
    string? Title,
    string? Comment,
    DateTime CreatedAt,
    bool IsVerifiedPurchase
);

public record CreateReviewDto(
    [Required] int ProductId,
    [Required, Range(1, 5)] int Rating,
    [StringLength(100)] string? Title,
    [StringLength(2000)] string? Comment
);

public class ZarinpalRequestDto
{
    [JsonPropertyName("merchant_id")]
    public required string MerchantID { get; set; }
    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }
    [JsonPropertyName("description")]
    public required string Description { get; set; }
    [JsonPropertyName("callback_url")]
    public required string CallbackURL { get; set; }
    [JsonPropertyName("metadata")]
    public ZarinpalMetadataDto? Metadata { get; set; }
}

public class ZarinpalMetadataDto
{
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class ZarinpalRequestResponseDto
{
    [JsonPropertyName("data")]
    public ZarinpalRequestResponseDataDto? Data { get; set; }
    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}

public class ZarinpalRequestResponseDataDto
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("authority")]
    public string? Authority { get; set; }
    [JsonPropertyName("fee_type")]
    public string? FeeType { get; set; }
    [JsonPropertyName("fee")]
    public decimal Fee { get; set; }
}

public class ZarinpalVerificationRequestDto
{
    [JsonPropertyName("merchant_id")]
    public required string MerchantID { get; set; }
    [JsonPropertyName("authority")]
    public required string Authority { get; set; }
    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }
}
public class ZarinpalVerificationResponseDto
{
    [JsonPropertyName("data")]
    public ZarinpalVerificationResponseDataDto? Data { get; set; }
    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}

public class ZarinpalVerificationResponseDataDto
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("card_hash")]
    public string? CardHash { get; set; }
    [JsonPropertyName("card_pan")]
    public string? CardPan { get; set; }
    [JsonPropertyName("ref_id")]
    public long RefID { get; set; }
    [JsonPropertyName("fee_type")]
    public string? FeeType { get; set; }
    [JsonPropertyName("fee")]
    public decimal Fee { get; set; }
}