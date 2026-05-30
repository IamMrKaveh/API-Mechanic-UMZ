using Application.Discount.Contracts;
using Domain.Discount.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Discount.Services;

public sealed class DiscountService(
    IDiscountRepository discountRepository,
    DBContext context,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IDiscountService
{
    public async Task<ServiceResult> ApplyDiscountAsync(
        string code,
        Money orderAmount,
        UserId userId,
        OrderId orderId,
        CancellationToken ct = default)
    {
        var discount = await discountRepository.GetByCodeAsync(code, ct);
        if (discount == null)
            return ServiceResult.Failure("کد تخفیف نامعتبر است.");

        var validation = discount.ValidateForApplication(orderAmount);
        if (!validation.IsValid)
            return ServiceResult.Failure(validation.FailureReason!);

        var discountAmount = discount.CalculateDiscount(orderAmount);
        discount.RecordUsage(userId, orderId, discountAmount);
        discountRepository.Update(discount);

        await auditService.LogOrderEventAsync(
            orderId,
            "DiscountApplied",
            IpAddress.Unknown,
            userId,
            $"Discount {code} applied. Amount: {discountAmount.Amount}",
            ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> CancelDiscountUsageAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        return await unitOfWork.ExecuteStrategyAsync(async (_, cancellationToken) =>
        {
            var discount = await context.DiscountCodes
                .Include(d => d.Usages)
                .FirstOrDefaultAsync(d => d.Usages.Any(u => u.OrderId == orderId), cancellationToken);

            if (discount == null)
                return ServiceResult.Success();

            discountRepository.Update(discount);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await auditService.LogSystemEventAsync(
                "DiscountUsageCancelled",
                $"Discount usage cancelled for order {orderId.Value}");

            return ServiceResult.Success();
        }, ct);
    }

    public async Task<ServiceResult> ConfirmDiscountUsageAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        return await unitOfWork.ExecuteStrategyAsync(async (_, cancellationToken) =>
        {
            var discount = await context.DiscountCodes
                .Include(d => d.Usages)
                .FirstOrDefaultAsync(d => d.Usages.Any(u => u.OrderId == orderId), cancellationToken);

            if (discount == null)
                return ServiceResult.Success();

            discountRepository.Update(discount);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await auditService.LogSystemEventAsync(
                "DiscountUsageConfirmed",
                $"Discount usage confirmed for order {orderId.Value}",
                cancellationToken);

            return ServiceResult.Success();
        }, ct);
    }
}