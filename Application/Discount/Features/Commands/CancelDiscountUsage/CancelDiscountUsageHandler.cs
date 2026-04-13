using Domain.Common.ValueObjects;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;

namespace Application.Discount.Features.Commands.CancelDiscountUsage;

public class CancelDiscountUsageHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<CancelDiscountUsageCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(CancelDiscountUsageCommand request, CancellationToken ct)
    {
        var discountCodeId = DiscountCodeId.From(request.DiscountCodeId);
        var orderId = OrderId.From(request.OrderId);

        var discount = await discountRepository.GetByIdWithUsagesAsync(discountCodeId, ct);
        if (discount is null)
            return ServiceResult.NotFound("کد تخفیف یافت نشد.");

        var usage = discount.Usages.FirstOrDefault(u => u.OrderId == orderId);
        if (usage is null)
            return ServiceResult.NotFound("استفاده‌ای برای این سفارش یافت نشد.");

        discountRepository.Update(discount);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "Discount",
            "CancelDiscountUsage",
            IpAddress.Unknown,
            null,
            "DiscountCode",
            request.DiscountCodeId.ToString(),
            $"لغو استفاده برای سفارش {request.OrderId}",
            null,
            ct);

        return ServiceResult.Success();
    }
}