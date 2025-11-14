namespace Domain.Category;

public class Category : BaseEntity
{
    public required string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Media.Media> Images { get; set; } = [];
    public ICollection<CategoryGroup> CategoryGroups { get; set; } = [];
}