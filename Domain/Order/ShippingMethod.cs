namespace Domain.Order;

public class ShippingMethod : ISoftDeletable, IActivatable
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public decimal Cost { get; set; }

    public string? Description { get; set; }

    public string? EstimatedDeliveryTime { get; set; }

    public ICollection<Order> Orders { get; set; } = [];

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
    public bool IsActive { get; set; } = true;
}