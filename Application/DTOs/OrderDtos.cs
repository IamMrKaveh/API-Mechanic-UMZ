namespace Application.DTOs;

public class CreateOrderDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public required string ReceiverName { get; set; }

    [Required]
    public int UserAddressId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ShippingMethodId must be a positive integer. ")]
    public int ShippingMethodId { get; set; }

    [Required]
    public int OrderStatusId { get; set; }

    public string? DiscountCode { get; set; }

    [Required]
    public List<CreateOrderItemDto> OrderItems { get; set; } = [];
}

public class UpdateOrderDto
{
    public int? OrderStatusId { get; set; }
    public int? ShippingMethodId { get; set; }
    public int? UserAddressId { get; set; }
    public DateTime? DeliveryDate { get; set; }

    [Required]
    public required string RowVersion { get; set; }
}

public class CreateOrderFromCartDto
{
    public int? UserAddressId { get; set; }
    public CreateUserAddressDto? NewAddress { get; set; }
    public bool SaveNewAddress { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ShippingMethodId must be a positive integer. ")]
    public int ShippingMethodId { get; set; }

    public string? DiscountCode { get; set; }

    [Required(ErrorMessage = "ExpectedItems is required for price validation.")]
    [MinLength(1, ErrorMessage = "ExpectedItems must contain at least one item. ")]
    public List<CheckoutItemPriceDto> ExpectedItems { get; set; } = [];

    public string? CallbackUrl { get; set; }
}

public class CreateOrderItemDto
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public int VariantId { get; set; }

    [Required]
    [Range(1, 1000)]
    public int Quantity { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal SellingPrice { get; set; }
}

public class UpdateOrderItemDto
{
    [Range(0, 1000)]
    public int? Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SellingPrice { get; set; }

    [Required]
    public required string RowVersion { get; set; }
}

public class CheckoutItemPriceDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "VariantId must be a positive integer.")]
    public int VariantId { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative.")]
    public decimal Price { get; set; }

    public string? RowVersion { get; set; }
}

public class CreateOrderStatusDto
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(50)]
    public string? Icon { get; set; }
}

public class UpdateOrderStatusDto
{
    public string? Name { get; set; }

    public string? Icon { get; set; }

    public string? RowVersion { get; set; }
}

public class UpdateOrderStatusByIdDto
{
    [Required]
    public int OrderStatusId { get; set; }

    [Required]
    public required string RowVersion { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? ReceiverName { get; set; }
    public UserAddressDto? UserAddress { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaymentDate { get; set; }
    public int OrderStatusId { get; set; }
    public string? OrderStatus { get; set; }
    public int ShippingMethodId { get; set; }
    public string? ShippingMethod { get; set; }
    public bool IsPaid { get; set; }
    public string? IdempotencyKey { get; set; }
    public ICollection<OrderItemDto> OrderItems { get; set; } = [];
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int VariantId { get; set; }
    public string? ProductName { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal Profit { get; set; }
}