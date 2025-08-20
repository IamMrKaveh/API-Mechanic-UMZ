namespace DataAccessLayer.Models.Order;

public class TOrderStatus : IOrderStatus
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }

    public virtual ICollection<TOrders>? Orders { get; set; }
}
