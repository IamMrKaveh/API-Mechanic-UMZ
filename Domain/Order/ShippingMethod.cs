namespace Domain.Order;

public class ShippingMethod
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public decimal Cost { get; set; }

    public string? Description { get; set; }

    public string? EstimatedDeliveryTime { get; set; }

    public ICollection<Order> Orders { get; set; } = [];
}