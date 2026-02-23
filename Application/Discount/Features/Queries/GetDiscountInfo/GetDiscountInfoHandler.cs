namespace Application.Discount.Features.Queries.GetDiscountInfo;

public class GetDiscountInfoHandler : IRequestHandler<GetDiscountInfoQuery, ServiceResult<DiscountInfoDto>>
{
    private readonly IDiscountRepository _discountRepository;

    public GetDiscountInfoHandler(
        IDiscountRepository discountRepository
        )
    {
        _discountRepository = discountRepository;
    }

    public async Task<ServiceResult<DiscountInfoDto>> Handle(
        GetDiscountInfoQuery request,
        CancellationToken ct
        )
    {
        var discount = await _discountRepository.GetByCodeAsync(request.Code, ct);
        if (discount == null)
            return ServiceResult<DiscountInfoDto>.Failure("کد تخفیف یافت نشد.");

        return ServiceResult<DiscountInfoDto>.Success(new DiscountInfoDto
        {
            Code = discount.Code.Value,
            Percentage = discount.Percentage,
            MaxDiscountAmount = discount.MaxDiscountAmount,
            MinOrderAmount = discount.MinOrderAmount,
            IsActive = discount.IsCurrentlyValid(),
            ExpiresAt = discount.ExpiresAt,
            StartsAt = discount.StartsAt,
            RemainingUsage = discount.RemainingUsage()
        });
    }
}