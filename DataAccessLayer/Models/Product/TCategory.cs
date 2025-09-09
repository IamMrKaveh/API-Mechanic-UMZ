namespace DataAccessLayer.Models.Product;

public class TCategory
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Icon { get; set; }

    public int ProductCount { get; set; }
    public long TotalValue { get; set; }
    public int InStockProducts { get; set; }
    public long TotalSellingValue { get; set; }

    public virtual ICollection<TProducts>? Products { get; set; }
}