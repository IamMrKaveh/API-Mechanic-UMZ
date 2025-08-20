namespace DataAccessLayer.Models.Order.Interfaces;

public interface IOrderItem
{
    public int Id { get; set; }

    public TOrders? UserOrder { get; set; }
    public int UserOrderId { get; set; }

    public TProducts? Product { get; set; }
    public int ProductId { get; set; }

    public int PurchasePrice { get; set; }
    public int SellingPrice { get; set; }
    public int Quantity { get; set; }
    public int Amount { get; set; }
    public int Profit { get; set; }
}