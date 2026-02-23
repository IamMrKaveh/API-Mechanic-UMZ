namespace Application.Discount.Features.Queries.GetDiscountUsageReport;

public record GetDiscountUsageReportQuery(int DiscountCodeId) : IRequest<ServiceResult<DiscountUsageReportDto>>;

public class DiscountUsageReportDto
{
    public int DiscountCodeId { get; init; }
    public string Code { get; init; } = string.Empty;
    public int TotalUsageCount { get; init; }
    public int? UsageLimit { get; init; }
    public int RemainingUsage { get; init; }
    public bool IsCurrentlyValid { get; init; }
    public IEnumerable<DiscountUsageItemDto> Usages { get; init; } = Enumerable.Empty<DiscountUsageItemDto>();
}

public record DiscountUsageItemDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string? UserName { get; init; }
    public int OrderId { get; init; }
    public decimal DiscountAmount { get; init; }
    public DateTime UsedAt { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsCancelled { get; init; }
}