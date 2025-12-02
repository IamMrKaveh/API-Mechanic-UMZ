namespace Application.DTOs;

public class CreateOrderDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string ReceiverName { get; set; }

    [Required]
    public int UserAddressId { get; set; }

    [Required]
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
    public string RowVersion { get; set; }
}

public class CreateOrderFromCartDto
{
    public int? UserAddressId { get; set; }
    public CreateUserAddressDto? NewAddress { get; set; }
    public bool SaveNewAddress { get; set; }

    [Required]
    public int ShippingMethodId { get; set; }

    public string? DiscountCode { get; set; }

    public List<CheckoutItemPriceDto> ExpectedItems { get; set; } = [];
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
    public string RowVersion { get; set; }
}

public class CheckoutItemPriceDto
{
    public int VariantId { get; set; }
    public decimal Price { get; set; }
}

public class CreateOrderStatusDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

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
    public string RowVersion { get; set; }
}