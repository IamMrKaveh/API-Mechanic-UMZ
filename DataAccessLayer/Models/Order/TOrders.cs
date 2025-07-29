namespace DataAccessLayer.Models.Order;

public class TOrders : IOrder
{
    [Key]
    public int Id { get; set; }

    public virtual TUsers? User { get; set; }
    public int? UserId { get; set; }

    public virtual TOrderDetails? OrderDetail { get; set; }

    public int? TotalAmount { get; set; }
    public int? TotalProfit { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public bool IsDelivered { get; set; } = false;
}