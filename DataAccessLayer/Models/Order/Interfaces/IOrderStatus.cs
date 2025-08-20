namespace DataAccessLayer.Models.Order.Interfaces;

public interface IOrderStatus
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }

    public ICollection<TOrders>? Orders
    { get; set; }
}
