namespace Application.Discount.Features.Queries.GetDiscountUsageReport;

public record GetDiscountUsageReportQuery(Guid DiscountCodeId) : IRequest<ServiceResult<DiscountUsageReportDto?>>;