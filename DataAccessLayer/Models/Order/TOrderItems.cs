namespace DataAccessLayer.Models.Order;

public class TOrderItems
{
    [Key]
    public int Id { get; set; }
    public virtual TOrders? UserOrder { get; set; }
    public int UserOrderId { get; set; }
    public virtual TProducts? Product { get; set; }
    public int ProductId { get; set; }
    public int PurchasePrice { get; set; }
    public int SellingPrice { get; set; }
    public int Quantity { get; set; }

    public int Amount { get; set; }

    public int Profit {  get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}