namespace Domain.Product;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty; 
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public int CategoryGroupId { get; set; }
    public CategoryGroup CategoryGroup { get; set; } = null!;

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>(); 
    public ICollection<Media.Media> Images { get; set; } = new List<Media.Media>(); 
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>(); 
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // Aggregated fields for performance
    public decimal MinPrice { get; private set; }
    public decimal MaxPrice { get; private set; }
    public int TotalStock { get; private set; }
    public bool HasMultipleVariants { get; private set; }

    public void RecalculateAggregates()
    {
        if (Variants == null || !Variants.Any())
        {
            MinPrice = 0;
            MaxPrice = 0;
            TotalStock = 0;
            HasMultipleVariants = false;
            return;
        }

        var activeVariants = Variants.Where(v => !v.IsDeleted && v.IsActive).ToList();

        if (activeVariants.Any())
        {
            MinPrice = activeVariants.Min(v => v.SellingPrice);
            MaxPrice = activeVariants.Max(v => v.SellingPrice);
            TotalStock = activeVariants.Sum(v => v.StockQuantity);
        }
        else
        {
            MinPrice = 0;
            MaxPrice = 0;
            TotalStock = 0;
        }

        HasMultipleVariants = Variants.Count > 1;
    }
}