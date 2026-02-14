namespace Application.Discount.Features.Queries.GetDiscountInfo;

/// <summary>
/// اطلاعات تخفیف برای نمایش به کاربر (بدون اطلاعات حساس ادمین)
/// </summary>
public record GetDiscountInfoQuery(string Code) : IRequest<ServiceResult<DiscountInfoDto>>;

public class DiscountInfoDto
{
    public string Code { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? StartsAt { get; set; }
    public int RemainingUsage { get; set; }
}