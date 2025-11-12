namespace DataAccessLayer.Models.Discount;

[Index(nameof(Code), IsUnique = true)]
[Index(nameof(ExpiresAt))]
[Index(nameof(IsActive))]
public class TDiscountCode : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "کد تخفیف الزامی است")]
    [StringLength(50, ErrorMessage = "کد تخفیف نمی‌تواند بیشتر از 50 کاراکتر باشد")]
    public string Code { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "درصد تخفیف باید بین 0 تا 100 باشد")]
    public decimal Percentage { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    [Range(0, double.MaxValue, ErrorMessage = "مبلغ تخفیف نمی‌تواند منفی باشد")]
    public decimal? MaxDiscountAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    [Range(0, double.MaxValue, ErrorMessage = "حداقل مبلغ سفارش نمی‌تواند منفی باشد")]
    public decimal? MinOrderAmount { get; set; }

    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public virtual ICollection<TDiscountRestriction> Restrictions { get; set; } = new List<TDiscountRestriction>();
    public virtual ICollection<TDiscountUsage> Usages { get; set; } = new List<TDiscountUsage>();
}