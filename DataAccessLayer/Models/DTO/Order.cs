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
}

public class CreateOrderStatusDto
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
}

public class UpdateOrderStatusDto
{
    public int OrderStatusId { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
}

public class CreateOrderItemDto
{
    public int UserOrderId { get; set; }
    public int ProductId { get; set; }
    public int SellingPrice { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderItemDto
{
    public int? SellingPrice { get; set; }
    public int? Quantity { get; set; }
}
