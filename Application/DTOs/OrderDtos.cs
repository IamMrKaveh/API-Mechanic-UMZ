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
}

public class UpdateOrderStatusByIdDto
{
    [Required]
    public int OrderStatusId { get; set; }

    [Required]
    public string RowVersion { get; set; }
}

public record ApplyDiscountDto(string Code, decimal OrderTotal);

public class ShippingMethodDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public decimal Cost { get; set; }

    public string? Description { get; set; }

    public string? EstimatedDeliveryTime { get; set; }

    public ICollection<Order> Orders { get; set; } = [];

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
    public bool IsActive { get; set; } = true;
}