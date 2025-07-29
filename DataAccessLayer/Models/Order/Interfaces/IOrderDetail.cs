namespace DataAccessLayer.Models.Order.Interfaces;

public interface IOrderDetail
{
    public int Id { get; set; }

    public TOrders? UserOrder { get; set; }
    public int? UserOrderId { get; set; }

    public ICollection<TProducts>? Products
    { get; set; }
    public ICollection<int>? ProductIds { get; set; }

    public ICollection<int>? PurchasePrice { get; set; }
    public ICollection<int>? SellingPrice { get; set; }
    public ICollection<int>? Quantity { get; set; }
    public ICollection<int>? Amount { get; set; }
    public ICollection<int>? Profit { get; set; }
}