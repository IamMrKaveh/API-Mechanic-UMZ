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
        
        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                
                var discount = await _discountRepository.GetByCodeAsync(request.Code, ct);

                if (discount == null)
                    return ServiceResult<DiscountApplyResultDto>.Failure("کد تخفیف نامعتبر است.");

                
                var userUsageCount = await _discountRepository.CountUserUsageAsync(discount.Id, request.UserId, ct);

                
                var (isValid, error) = discount.Validate(request.OrderTotal, request.UserId, userUsageCount);
                if (!isValid)
                {
                    return ServiceResult<DiscountApplyResultDto>.Failure(error!);
                }

                
                var discountAmount = discount.CalculateDiscountAmount(request.OrderTotal);

                
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