namespace DataAccessLayer.Models.Product;

public class TProducts : IProduct
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
    public string? Icon { get; set; }

    public int? PurchasePrice { get; set; }
    public int? SellingPrice { get; set; }
    public int? Count { get; set; }

    public virtual TProductTypes? ProductType { get; set; }
    public int? ProductTypeId { get; set; }

    public virtual ICollection<TOrderItems>? OrderDetails
    { get; set; }
}