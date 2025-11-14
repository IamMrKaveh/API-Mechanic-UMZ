namespace Domain.Category;

public class CategoryGroup : BaseEntity
{
    public required string Name { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public ICollection<Product.Product> Products { get; set; } = [];
    public ICollection<Media.Media> Images { get; set; } = [];
}