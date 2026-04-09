using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;

namespace Application.Discount.Features.Commands.CancelDiscountUsage;

public class CancelDiscountUsageHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    ILogger<CancelDiscountUsageHandler> logger) : IRequestHandler<CancelDiscountUsageCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(CancelDiscountUsageCommand request, CancellationToken ct)
    {
        try
        {
            var discount = await discountRepository.GetByIdWithUsagesAsync(
                DiscountCodeId.From(request.DiscountCodeId), ct);

            if (discount is null)
                return ServiceResult.NotFound("کد تخفیف یافت نشد.");

            discountRepository.Update(discount);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel discount usage for order {OrderId}", request.OrderId);
            return ServiceResult.Failure("خطا در لغو استفاده از کد تخفیف.");
        }
    }
}