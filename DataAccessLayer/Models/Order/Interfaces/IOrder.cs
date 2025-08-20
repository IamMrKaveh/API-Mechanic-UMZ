namespace DataAccessLayer.Models.Order.Interfaces;

public interface IOrder
{
    public int Id { get; set; }

    public TUsers? User { get; set; }
    public int UserId { get; set; }

    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }

    public ICollection<TOrderItems> OrderItems { get; set; }

    public int TotalAmount { get; set; }
    public int TotalProfit { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveryDate { get; set; }

    public TOrderStatus? OrderStatus { get; set; }
    public int OrderStatusId { get; set; }
}