using Domain.Discount.Interfaces;

namespace Infrastructure.Discount.Services;

public class DiscountService(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    ILogger<DiscountService> logger) : IDiscountService
{
    private readonly IDiscountRepository _discountRepository = discountRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<DiscountService> _logger = logger;

    public async Task<ServiceResult<DiscountApplyResultDto>> ValidateAndApplyDiscountAsync(
        string code,
        decimal orderTotal,
        int userId,
        CancellationToken ct = default)
    {
        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var now = DateTime.UtcNow;
                var discount = await _discountRepository.GetByCodeAsync(code, ct);
                if (discount == null)
                    return ServiceResult<DiscountApplyResultDto>.Failure("کد تخفیف نامعتبر است.");

                var userUsageCount = await _discountRepository.CountUserUsageAsync(discount.Id, userId, ct);
                var (isValid, error) = discount.Validate(orderTotal, now, userId, userUsageCount);

                if (!isValid)
                    return ServiceResult<DiscountApplyResultDto>.Failure(error!);

                var discountAmount = discount.CalculateDiscountAmount(orderTotal, now);

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

    public async Task<ServiceResult> CancelDiscountUsageAsync(
        int orderId,
        CancellationToken ct = default)
    {
        var usage = await _discountRepository.GetUsageByOrderIdAsync(orderId, ct);
        if (usage == null)
            return ServiceResult.Success();

        var discount = await _discountRepository.GetByIdWithUsagesAsync(usage.DiscountCodeId, ct);
        if (discount == null)
            return ServiceResult.Failure("کد تخفیف یافت نشد.");

        discount.CancelUsage(orderId);
        _discountRepository.Update(discount);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Discount usage cancelled for order {OrderId}, discount {DiscountId}",
            orderId, discount.Id);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ConfirmDiscountUsageAsync(
        int orderId,
        CancellationToken ct = default)
    {
        var usage = await _discountRepository.GetUsageByOrderIdAsync(orderId, ct);
        if (usage == null)
            return ServiceResult.Success();

        var discount = await _discountRepository.GetByIdWithUsagesAsync(usage.DiscountCodeId, ct);
        if (discount == null)
            return ServiceResult.Failure("کد تخفیف یافت نشد.");

        discount.ConfirmUsage(orderId);
        _discountRepository.Update(discount);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Discount usage confirmed for order {OrderId}, discount {DiscountId}",
            orderId, discount.Id);

        return ServiceResult.Success();
    }
}