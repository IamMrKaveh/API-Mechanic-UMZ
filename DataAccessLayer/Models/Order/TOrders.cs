namespace DataAccessLayer.Models.Order;

public class TOrders : IOrder
{
    [Key]
    public int Id { get; set; }

    public virtual TUsers? User { get; set; }
    public int UserId { get; set; }
    public string? Name { get; set; }

    public string? Address { get; set; }
    public string? PostalCode { get; set; }

    public virtual ICollection<TOrderItems> OrderItems 
    { get; set; } = new List<TOrderItems>();

    public int TotalAmount { get; set; }
    public int TotalProfit { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveryDate { get; set; }

    public virtual TOrderStatus? OrderStatus { get; set; }
    public int OrderStatusId { get; set; }
}