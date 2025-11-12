namespace DataAccessLayer.Models.DTO;

public class CreateOrderDto
{
    public int UserId { get; set; }
    public int UserAddressId { get; set; }
    public int OrderStatusId { get; set; }
    public DateTime? DeliveryDate { get; set; }

    [Required(ErrorMessage = "روش ارسال الزامی است")]
    public int ShippingMethodId { get; set; }

    public string? DiscountCode { get; set; }

    public List<CreateOrderItemDto> OrderItems { get; set; } = new();
}

public class UpdateOrderDto
{
    public int? UserAddressId { get; set; }
    public int? OrderStatusId { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public int? ShippingMethodId { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class CreateOrderStatusDto
{
    [Required]
    public string? Name { get; set; }
}

public class UpdateOrderStatusDto
{
    [Required]
    public int OrderStatusId { get; set; }
    public string? Name { get; set; }
}

public class CreateOrderItemDto
{
    public int OrderId { get; set; }
    public int VariantId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "قیمت فروش باید بزرگتر از 0 باشد")]
    public decimal SellingPrice { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "تعداد باید بزرگتر از 0 باشد")]
    public int Quantity { get; set; }
}

public class UpdateOrderItemDto
{
    public decimal? SellingPrice { get; set; }
    public int? Quantity { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class CreateOrderFromCartDto
{
    [Required(ErrorMessage = "شناسه آدرس کاربر الزامی است")]
    public int UserAddressId { get; set; }

    [Required(ErrorMessage = "روش ارسال الزامی است")]
    public int ShippingMethodId { get; set; }

    public string? DiscountCode { get; set; }
}

public class ShippingMethodDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام الزامی است")]
    [StringLength(100, ErrorMessage = "نام نمی‌تواند بیشتر از 100 کاراکتر باشد")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public decimal Cost { get; set; }
    public string? EstimatedDeliveryTime { get; set; }
}

public class PublicOrderViewDto
{
    public int Id { get; set; }
    public UserAddressDto Address { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool IsPaid { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string ShippingMethodName { get; set; } = string.Empty;
    public decimal ShippingMethodCost { get; set; }
    public string OrderStatusName { get; set; } = string.Empty;
    public List<PublicOrderItemViewDto> OrderItems { get; set; } = new();
}

public class PublicOrderItemProductViewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? CategoryName { get; set; }
    public string? ColorName { get; set; }
    public string? SizeName { get; set; }
}

public class PublicOrderItemOrderViewDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PublicOrderItemViewDto
{
    public int Id { get; set; }
    public decimal SellingPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public PublicOrderItemProductViewDto? Product { get; set; }
    public PublicOrderItemOrderViewDto? Order { get; set; }
}

public class PublicOrderItemDetailDto
{
    public int Id { get; set; }
    public decimal SellingPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public PublicOrderItemProductViewDto? Product { get; set; }
    public PublicOrderItemOrderViewDto? Order { get; set; }
}

public class AdminOrderItemDetailDto : PublicOrderItemDetailDto
{
    public decimal PurchasePrice { get; set; }
    public decimal Profit { get; set; }
}