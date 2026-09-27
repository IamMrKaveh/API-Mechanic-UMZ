using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;

namespace Application.Discount.Features.Commands.CancelDiscountUsage;

public class CancelDiscountUsageHandler(
    IDiscountRepository discountRepository,
    IAuditService auditService)
    : ICommandHandler<CancelDiscountUsageCommand>
{
    public async Task<ServiceResult> Handle(CancelDiscountUsageCommand request, CancellationToken ct)
    {
        var discountCodeId = DiscountCodeId.From(request.DiscountCodeId);
        var orderId = OrderId.From(request.OrderId);

        var discountResult = await (discountRepository.GetByIdWithUsagesAsync(discountCodeId, ct)).OrNotFoundAsync("کد تخفیف یافت نشد.");
        if (discountResult.IsFailure) return discountResult.Error;
        var discount = discountResult.Value;

        var usage = discount.Usages.FirstOrDefault(u => u.OrderId == orderId);
        if (usage is null)
            return ServiceResult.NotFound("استفاده‌ای برای این سفارش یافت نشد.");

        discountRepository.Update(discount);

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