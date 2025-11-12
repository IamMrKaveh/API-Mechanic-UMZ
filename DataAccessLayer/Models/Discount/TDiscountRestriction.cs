namespace DataAccessLayer.Models.Discount;

public class TDiscountRestriction
{
    [Key]
    public int Id { get; set; }

    public int DiscountCodeId { get; set; }
    public virtual TDiscountCode DiscountCode { get; set; } = null!;

    [MaxLength(50)]
    public string RestrictionType { get; set; } = string.Empty;
    // User, Category, Product, UserGroup

    public int? EntityId { get; set; }
}