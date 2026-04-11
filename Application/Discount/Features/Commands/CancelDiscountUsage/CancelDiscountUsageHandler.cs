using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Common.ValueObjects;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using MediatR;

namespace Application.Discount.Features.Commands.CancelDiscountUsage;

public class CancelDiscountUsageHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<CancelDiscountUsageCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(CancelDiscountUsageCommand request, CancellationToken ct)
    {
        return await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var discountCodeId = DiscountCodeId.From(request.DiscountCodeId);
                var orderId = OrderId.From(request.OrderId);

                var discount = await discountRepository.GetByIdWithUsagesAsync(discountCodeId, ct);
                if (discount is null)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult.NotFound("کد تخفیف یافت نشد.");
                }

                discount.CancelUsage(orderId);

                discountRepository.Update(discount);
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                await auditService.LogAsync(
                    "DiscountUsage",
                    "CancelDiscountUsage",
                    IpAddress.Unknown,
                    details: $"Discount usage cancelled for order {request.OrderId}",
                    ct: ct);

                return ServiceResult.Success();
            }
            catch (Exception)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return ServiceResult.Failure("خطا در لغو استفاده از کد تخفیف.");
            }
        }, ct);
    }
}