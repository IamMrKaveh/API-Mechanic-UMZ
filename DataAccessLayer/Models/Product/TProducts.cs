namespace DataAccessLayer.Models.Product;

public class TProducts
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Icon { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SellingPrice { get; set; }

    public bool HasDiscount => OriginalPrice > SellingPrice;

    public double DiscountPercentage
    {
        get
        {
            if (HasDiscount && OriginalPrice > 0)
            {
                return Math.Max(0, ((double)(OriginalPrice - SellingPrice)) * 100 / ((double)OriginalPrice));
            }
            return 0;
        }
    }

    public int Count { get; set; }
    public bool IsUnlimited { get; set; } = false;
    public virtual TCategory? Category { get; set; }
    public int CategoryId { get; set; }
    public virtual ICollection<TOrderItems>? OrderDetails { get; set; }
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}