namespace Application.Discount.Features.Queries.GetDiscountUsageReport;

public class GetDiscountUsageReportHandler(IDiscountQueryService discountQueryService) : IRequestHandler<GetDiscountUsageReportQuery, ServiceResult<DiscountUsageReportDto?>>
{
    private readonly IDiscountQueryService _discountQueryService = discountQueryService;

    public async Task<ServiceResult<DiscountUsageReportDto?>> Handle(
        GetDiscountUsageReportQuery request,
        CancellationToken ct)
    {
        var report = await _discountQueryService.GetUsageReportByIdAsync(request.DiscountCodeId, ct);
        return report is null
            ? ServiceResult<DiscountUsageReportDto?>.NotFound("کد تخفیف یافت نشد.")
            : ServiceResult<DiscountUsageReportDto?>.Success(report);
    }
}