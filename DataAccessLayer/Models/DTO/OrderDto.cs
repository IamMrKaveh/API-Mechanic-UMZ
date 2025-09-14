namespace DataAccessLayer.Models.DTO;

public class CreateOrderDto
{
    public int UserId { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public int OrderStatusId { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public List<CreateOrderItemDto> OrderItems { get; set; } = new();
}

public class UpdateOrderDto
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public int? OrderStatusId { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class CreateOrderStatusDto
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
}

public class CreateOrderFromCartDto
{
    public string? Name { get; set; }

    [Required]
    public string? Address { get; set; }

    [Required]
    public string? PostalCode { get; set; }
}

public class UpdateOrderStatusDto
{
    [Required]
    public int OrderStatusId { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }
}

public class CreateOrderItemDto
{
    public int UserOrderId { get; set; }
    public int ProductId { get; set; }
    [Range(1, int.MaxValue)]
    public int SellingPrice { get; set; }
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class UpdateOrderItemDto
{
    public int? SellingPrice { get; set; }
    public int? Quantity { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class PublicOrderItemProductViewDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public string CategoryName { get; set; }
}

public class PublicOrderItemOrderViewDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PublicOrderItemViewDto
{
    public int Id { get; set; }
    public int SellingPrice { get; set; }
    public int Quantity { get; set; }
    public int Amount { get; set; }
    public PublicOrderItemProductViewDto Product { get; set; }
    public PublicOrderItemOrderViewDto Order { get; set; }
}

public class PublicOrderItemDetailDto
{
    public int Id { get; set; }
    public int SellingPrice { get; set; }
    public int Quantity { get; set; }
    public int Amount { get; set; }
    public PublicOrderItemProductViewDto Product { get; set; }
    public PublicOrderItemOrderViewDto Order { get; set; }
}

public class AdminOrderItemDetailDto : PublicOrderItemDetailDto
{
    public int PurchasePrice { get; set; }
    public int Profit { get; set; }
}