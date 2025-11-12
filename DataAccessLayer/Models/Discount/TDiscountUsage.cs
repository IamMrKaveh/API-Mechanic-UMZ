namespace DataAccessLayer.Models.Discount;

[Index(nameof(UserId), nameof(DiscountCodeId))]
public class TDiscountUsage
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public virtual TUsers User { get; set; } = null!;

    public int DiscountCodeId { get; set; }
    public virtual TDiscountCode DiscountCode { get; set; } = null!;

    public int OrderId { get; set; }
    public virtual TOrders Order { get; set; } = null!;

    public decimal DiscountAmount { get; set; }
    public DateTime UsedAt { get; set; }
}