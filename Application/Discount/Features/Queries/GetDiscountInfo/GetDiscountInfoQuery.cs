namespace Application.Discount.Features.Queries.GetDiscountInfo;

/// <summary>
/// اطلاعات تخفیف برای نمایش به کاربر (بدون اطلاعات حساس ادمین)
/// </summary>
public record GetDiscountInfoQuery(
    string Code
    ) : IRequest<ServiceResult<DiscountInfoDto>>;

public record DiscountInfoDto
{
    public string Code { get; init; } = string.Empty;
    public decimal Percentage { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public bool IsActive { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? StartsAt { get; init; }
    public int RemainingUsage { get; init; }
}