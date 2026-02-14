namespace Application.Discount.Features.Queries.GetDiscountUsageReport;

public class GetDiscountUsageReportHandler : IRequestHandler<GetDiscountUsageReportQuery, ServiceResult<DiscountUsageReportDto>>
{
    private readonly IDiscountRepository _discountRepository;

    public GetDiscountUsageReportHandler(IDiscountRepository discountRepository)
    {
        _discountRepository = discountRepository;
    }

    public async Task<ServiceResult<DiscountUsageReportDto>> Handle(GetDiscountUsageReportQuery request, CancellationToken cancellationToken)
    {
        var discount = await _discountRepository.GetByIdWithDetailsAsync(request.DiscountCodeId, cancellationToken);
        if (discount == null)
            return ServiceResult<DiscountUsageReportDto>.Failure("کد تخفیف یافت نشد.");

        var report = new DiscountUsageReportDto
        {
            DiscountCodeId = discount.Id,
            Code = discount.Code,
            TotalUsageCount = discount.UsedCount,
            UsageLimit = discount.UsageLimit,
            RemainingUsage = discount.RemainingUsage(),
            IsCurrentlyValid = discount.IsCurrentlyValid(),
            Usages = discount.Usages.Select(u => new DiscountUsageItemDto
            {
                Id = u.Id,
                UserId = u.UserId,
                UserName = u.User != null ? $"{u.User.FirstName} {u.User.LastName}" : null,
                OrderId = u.OrderId,
                DiscountAmount = u.DiscountAmount.Amount,
                UsedAt = u.UsedAt,
                IsConfirmed = u.IsConfirmed,
                IsCancelled = u.IsCancelled
            })
        };

        return ServiceResult<DiscountUsageReportDto>.Success(report);
    }
}