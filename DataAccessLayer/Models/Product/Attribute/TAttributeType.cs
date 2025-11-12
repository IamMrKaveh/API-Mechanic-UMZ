namespace DataAccessLayer.Models.Product.Attribute;

[Index(nameof(Name), IsUnique = true)]
public class TAttributeType : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "نام نوع ویژگی الزامی است")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string DisplayName { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<TAttributeValue> AttributeValues { get; set; } = new List<TAttributeValue>();
}