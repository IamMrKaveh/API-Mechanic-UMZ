using Application.Discount.Features.Shared;

namespace Application.Discount.Features.Queries.GetDiscountUsageReport;

public class GetDiscountUsageReportHandler(IDiscountQueryService discountQueryService) : IRequestHandler<GetDiscountUsageReportQuery, ServiceResult<DiscountUsageReportDto?>>
{
    public async Task<ServiceResult<DiscountUsageReportDto?>> Handle(
        GetDiscountUsageReportQuery request,
        CancellationToken ct)
    {
        var report = await discountQueryService.GetUsageReportByIdAsync(request.DiscountCodeId, ct);
        return report is null
            ? ServiceResult<DiscountUsageReportDto?>.NotFound("کد تخفیف یافت نشد.")
            : ServiceResult<DiscountUsageReportDto?>.Success(report);
    }
}