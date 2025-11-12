namespace DataAccessLayer.Models.DTO;

public class DiscountCodeDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "کد تخفیف الزامی است")]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Range(0, 100)]
    public decimal Percentage { get; set; }

    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? UserId { get; set; }
}

public class CreateDiscountCodeDto
{
    [Required(ErrorMessage = "کد تخفیف الزامی است")]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Range(0.01, 100)]
    public decimal Percentage { get; set; }

    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? UsageLimit { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? UserId { get; set; }
}

public class ApplyDiscountDto
{
    [Required(ErrorMessage = "کد تخفیف الزامی است")]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal OrderTotal { get; set; }
}