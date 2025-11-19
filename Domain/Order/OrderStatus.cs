namespace Domain.Order;

public class OrderStatus : ISoftDeletable, IActivatable
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Icon { get; set; }

    public ICollection<Order> Orders { get; set; } = [];

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
    public bool IsActive { get; set; } = true;
}