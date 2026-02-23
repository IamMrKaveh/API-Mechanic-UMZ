namespace Application.Discount.Features.Queries.ValidateDiscount;

public class ValidateDiscountHandler : IRequestHandler<ValidateDiscountQuery, ServiceResult<DiscountValidationDto>>
{
    private readonly IDiscountRepository _discountRepository;

    public ValidateDiscountHandler(
        IDiscountRepository discountRepository
        )
    {
        _discountRepository = discountRepository;
    }

    public async Task<ServiceResult<DiscountValidationDto>> Handle(
        ValidateDiscountQuery request,
        CancellationToken ct
        )
    {
        var discount = await _discountRepository.GetByCodeAsync(request.Code);
        if (discount == null)
            return ServiceResult<DiscountValidationDto>.Failure("کد تخفیف نامعتبر است.");

        var userUsageCount = await _discountRepository.CountUserUsageAsync(discount.Id, request.UserId);
        var (isValid, error) = discount.Validate(request.OrderTotal, request.UserId, userUsageCount);

        if (!isValid)
        {
            return ServiceResult<DiscountValidationDto>.Success(new DiscountValidationDto
            {
                IsValid = false,
                Message = error,
                EstimatedDiscount = 0
            });
        }

        var amount = discount.CalculateDiscountAmount(request.OrderTotal);

        return ServiceResult<DiscountValidationDto>.Success(new DiscountValidationDto
        {
            IsValid = true,
            EstimatedDiscount = amount,
            Message = "کد تخفیف معتبر است."
        });
    }
}