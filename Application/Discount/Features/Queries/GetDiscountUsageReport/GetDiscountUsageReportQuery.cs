using Application.Common.Models;

namespace Application.Discount.Features.Queries.GetDiscountUsageReport;

public record GetDiscountUsageReportQuery(int DiscountCodeId) : IRequest<ServiceResult<DiscountUsageReportDto>>;