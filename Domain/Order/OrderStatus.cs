namespace Domain.Order;

public class OrderStatus
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Icon { get; set; }

    public ICollection<Order> Orders { get; set; } = [];
}