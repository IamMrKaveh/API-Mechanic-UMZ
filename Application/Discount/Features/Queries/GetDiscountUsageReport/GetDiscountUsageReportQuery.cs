using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.GetDiscountUsageReport;

public record GetDiscountUsageReportQuery(
    Guid DiscountCodeId) : IQuery<DiscountUsageReportDto?>;