namespace DataAccessLayer.Models.Product;

public class TProductTypes : IProductType
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
    public string? Icon { get; set; }

    public virtual ICollection<TProducts>? Products { get; set; }
}