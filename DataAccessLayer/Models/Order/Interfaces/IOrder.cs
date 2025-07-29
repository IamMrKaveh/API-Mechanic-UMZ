namespace DataAccessLayer.Models.Order.Interfaces;

public interface IOrder
{
    public int Id { get; set; }

    public TUsers? User { get; set; }
    public int? UserId { get; set; }

    public TOrderDetails? OrderDetail
    { get; set; }

    public int? TotalAmount { get; set; }
    public int? TotalProfit { get; set; }

    public DateTime? PurchaseDate { get; set; }
}