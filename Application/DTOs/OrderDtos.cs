namespace Application.DTOs;

public class CreateOrderDto
{
    [Required]
    public int UserId { get; set; }

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
    public required byte[] RowVersion { get; set; }
}

public class CreateOrderFromCartDto
{
    [Required]
    public int UserAddressId { get; set; }

    [Required]
    public int ShippingMethodId { get; set; }

    public string? DiscountCode { get; set; }
}

public class UpdateOrderStatusByIdDto
{
    [Required]
    public int OrderStatusId { get; set; }
}

public record ApplyDiscountDto(string Code, decimal OrderTotal);