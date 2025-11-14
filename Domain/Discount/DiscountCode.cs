namespace Domain.Discount;

public class DiscountCode : IAuditable
{
    public int Id { get; set; }

    public required string Code { get; set; }

    public decimal Percentage { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public decimal? MinOrderAmount { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public byte[]? RowVersion { get; set; }

    public ICollection<DiscountRestriction> Restrictions { get; set; } = [];
    public ICollection<DiscountUsage> Usages { get; set; } = [];
    public ICollection<Order.Order> Orders { get; set; } = [];
}