namespace DataAccessLayer.Models.Order;

public class TOrderItems
{
    [Key]
    public int Id { get; set; }
    public virtual TOrders? UserOrder { get; set; }
    public int UserOrderId { get; set; }
    public virtual TProducts? Product { get; set; }
    public int ProductId { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int Quantity { get; set; }

    public decimal Amount { get; set; }

    public decimal Profit {  get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}