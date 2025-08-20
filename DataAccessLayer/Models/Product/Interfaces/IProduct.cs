namespace DataAccessLayer.Models.Product.Interfaces;

public interface IProduct : IBaseEntity
{
    public string? Icon { get; set; }
    public int? PurchasePrice { get; set; }
    public int? SellingPrice { get; set; }
    public int? Count { get; set; }

    public TProductTypes? ProductType { get; set; }
    public int? ProductTypeId { get; set; }

    public ICollection<TOrderItems>? OrderDetails
    { get; set; }
}