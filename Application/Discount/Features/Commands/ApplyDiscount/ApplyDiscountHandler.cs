namespace Application.Discount.Features.Commands.ApplyDiscount;

public class ApplyDiscountHandler : IRequestHandler<ApplyDiscountCommand, ServiceResult<DiscountApplyResultDto>>
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyDiscountHandler(
        IDiscountRepository discountRepository,
        IUnitOfWork unitOfWork
        )
    {
        _discountRepository = discountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<DiscountApplyResultDto>> Handle(
        ApplyDiscountCommand request,
        CancellationToken ct
        )
    {
        // استفاده از Strategy برای جلوگیری از Race Condition در شمارش استفاده‌ها
        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                // قفل کردن رکورد برای آپدیت امن شمارنده (SELECT FOR UPDATE)
                var discount = await _discountRepository.GetByCodeAsync(request.Code, ct);

                if (discount == null)
                    return ServiceResult<DiscountApplyResultDto>.Failure("کد تخفیف نامعتبر است.");

                // دریافت تعداد استفاده قبلی کاربر
                var userUsageCount = await _discountRepository.CountUserUsageAsync(discount.Id, request.UserId, ct);

                // اعتبارسنجی توسط متد غنی Domain
                var (isValid, error) = discount.Validate(request.OrderTotal, request.UserId, userUsageCount);
                if (!isValid)
                {
                    return ServiceResult<DiscountApplyResultDto>.Failure(error!);
                }

                // محاسبه مبلغ تخفیف توسط Domain
                var discountAmount = discount.CalculateDiscountAmount(request.OrderTotal);

                // افزایش شمارنده توسط متد Domain
                discount.IncrementUsage();
                _discountRepository.Update(discount);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return ServiceResult<DiscountApplyResultDto>.Success(new DiscountApplyResultDto
                {
                    DiscountCodeId = discount.Id,
                    DiscountAmount = discountAmount,
                    Code = discount.Code.Value
                });
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, ct);
    }
}