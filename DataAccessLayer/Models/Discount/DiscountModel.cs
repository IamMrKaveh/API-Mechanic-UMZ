namespace DataAccessLayer.Models.Discount;

[Index(nameof(Code), IsUnique = true)]
[Index(nameof(ExpiresAt), nameof(IsActive))]
[Index(nameof(IsActive))]
public class TDiscountCode : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(50)]
    public required string Code { get; set; }

    [Required, Range(0, 100)]
    [Column(TypeName = "decimal(5,2)")]
    public decimal Percentage { get; set; }

    [Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal? MaxDiscountAmount { get; set; }

    [Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal? MinOrderAmount { get; set; }

    [Range(1, int.MaxValue)]
    public int? UsageLimit { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int UsedCount { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<TDiscountRestriction> Restrictions { get; set; } = [];
    public ICollection<TDiscountUsage> Usages { get; set; } = [];
    public ICollection<TOrders> Orders { get; set; } = [];
}

[Index(nameof(DiscountCodeId), nameof(RestrictionType), nameof(EntityId))]
public class TDiscountRestriction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DiscountCodeId { get; set; }
    public TDiscountCode DiscountCode { get; set; } = null!;

    [Required, MaxLength(50)]
    public required string RestrictionType { get; set; }

    public int? EntityId { get; set; }
}

[Index(nameof(UserId), nameof(DiscountCodeId), nameof(OrderId))]
[Index(nameof(DiscountCodeId), nameof(UsedAt))]
public class TDiscountUsage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public TUsers User { get; set; } = null!;

    [Required]
    public int DiscountCodeId { get; set; }
    public TDiscountCode DiscountCode { get; set; } = null!;

    [Required]
    public int OrderId { get; set; }
    public TOrders Order { get; set; } = null!;

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Required]
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}