namespace Domain.Product;

public class Product : BaseEntity
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    public string? Sku { get; set; }

    public int CategoryGroupId { get; set; }
    public Category.CategoryGroup CategoryGroup { get; set; } = null!;

    public new bool IsActive { get; set; } = true;

    public decimal MinPrice { get; set; }

    public decimal MaxPrice { get; set; }

    public int TotalStock { get; set; }

    public bool HasMultipleVariants => Variants.Count > 1;

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public ICollection<Media.Media> Images { get; set; } = [];
    public ICollection<ProductReview> Reviews { get; set; } = [];

    public void RecalculateAggregates()
    {
        var activeVariants = Variants.Where(v => v.IsActive).ToList();

        if (activeVariants.Any())
        {
            MinPrice = activeVariants.Min(v => v.SellingPrice);
            MaxPrice = activeVariants.Max(v => v.SellingPrice);
            TotalStock = activeVariants.Where(v => !v.IsUnlimited).Sum(v => v.Stock);
        }
        else
        {
            MinPrice = 0;
            MaxPrice = 0;
            TotalStock = 0;
        }
    }
}