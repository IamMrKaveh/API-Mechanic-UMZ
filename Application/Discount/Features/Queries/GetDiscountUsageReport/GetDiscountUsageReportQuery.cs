using Application.Common.Results;
using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.GetDiscountUsageReport;

public record GetDiscountUsageReportQuery(int DiscountCodeId) : IRequest<ServiceResult<DiscountUsageReportDto?>>;