using Application.Discount.Features.Shared;
using Domain.Discount.ValueObjects;

namespace Application.Discount.Features.Queries.GetDiscountUsageReport;

public class GetDiscountUsageReportHandler(IDiscountQueryService discountQueryService) : IRequestHandler<GetDiscountUsageReportQuery, ServiceResult<DiscountUsageReportDto?>>
{
    public async Task<ServiceResult<DiscountUsageReportDto?>> Handle(
        GetDiscountUsageReportQuery request,
        CancellationToken ct)
    {
        var discountCodeId = DiscountCodeId.From(request.DiscountCodeId);

        var report = await discountQueryService.GetUsageReportByIdAsync(discountCodeId, ct);
        return report is null
            ? ServiceResult<DiscountUsageReportDto?>.NotFound("کد تخفیف یافت نشد.")
            : ServiceResult<DiscountUsageReportDto?>.Success(report);
    }
}