namespace Application.Discount.Features.Queries.GetDiscountUsageReport;

public record GetDiscountUsageReportQuery(int DiscountCodeId) : IRequest<ServiceResult<DiscountUsageReportDto>>;

public class DiscountUsageReportDto
{
    public int DiscountCodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int TotalUsageCount { get; set; }
    public int? UsageLimit { get; set; }
    public int RemainingUsage { get; set; }
    public bool IsCurrentlyValid { get; set; }
    public IEnumerable<DiscountUsageItemDto> Usages { get; set; } = Enumerable.Empty<DiscountUsageItemDto>();
}

public class DiscountUsageItemDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public int OrderId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime UsedAt { get; set; }
    public bool IsConfirmed { get; set; }
    public bool IsCancelled { get; set; }
}