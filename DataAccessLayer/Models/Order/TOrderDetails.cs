public class TOrderDetails : IOrderDetail
{
    [Key]
    public int Id { get; set; }

    public virtual TOrders? UserOrder { get; set; }
    public int? UserOrderId { get; set; }

    public virtual ICollection<TProducts>? Products 
    { get; set; }

    [NotMapped]
    public ICollection<int>? ProductIds { get; set; }

    public ICollection<int>? PurchasePrice { get; set; }
    public ICollection<int>? SellingPrice { get; set; }
    public ICollection<int>? Quantity { get; set; }
    public ICollection<int>? Amount { get; set; }
    public ICollection<int>? Profit { get; set; }

}
