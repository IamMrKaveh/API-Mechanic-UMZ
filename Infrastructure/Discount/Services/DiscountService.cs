using Application.Discount.Contracts;
using Domain.Discount.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Infrastructure.Discount.Services;

public sealed class DiscountService(
    IDiscountRepository discountRepository,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IDiscountService
{
    public async Task<ServiceResult> ApplyDiscountAsync(
        string code,
        Money orderAmount,
        UserId userId,
        OrderId orderId,
        CancellationToken ct = default)
    {
        var discount = await discountRepository.GetByCodeAsync(code, ct);
        if (discount is null)
            return ServiceResult.Failure("کد تخفیف نامعتبر است.");

        var validation = discount.ValidateForApplication(orderAmount, dateTimeProvider.UtcNow);
        if (validation.IsValid is false)
            return ServiceResult.Failure(validation.FailureReason!);

        var discountAmount = discount.CalculateDiscount(orderAmount);
        discount.RecordUsage(userId, orderId, discountAmount, dateTimeProvider.UtcNow);
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
}