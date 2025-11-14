namespace Domain.Product.Attribute;

public class AttributeType : IAuditable
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string DisplayName { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<AttributeValue> AttributeValues { get; set; } = [];
}