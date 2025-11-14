namespace Domain.Discount;

public class DiscountUsage
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User.User User { get; set; } = null!;

    public int DiscountCodeId { get; set; }
    public DiscountCode DiscountCode { get; set; } = null!;

    public int OrderId { get; set; }
    public Order.Order Order { get; set; } = null!;

    public decimal DiscountAmount { get; set; }

    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}