using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Discount.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Infrastructure.Order.Services;

public sealed class CheckoutDiscountApplicatorService(
    IDiscountRepository discountRepository,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : ICheckoutDiscountApplicatorService
{
    public async Task<ServiceResult<(Money DiscountAmount, Guid? DiscountCodeId)>> ApplyAsync(
        string? discountCode, Money orderAmount, Guid userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(discountCode))
            return ServiceResult<(Money, Guid?)>.Success((Money.Zero(orderAmount.Currency), null));

        var discount = await discountRepository.GetByCodeAsync(discountCode, ct);
        if (discount is null)
            return ServiceResult<(Money, Guid?)>.NotFound("کد تخفیف یافت نشد.");

        var validation = discount.ValidateForApplication(orderAmount, dateTimeProvider.UtcNow);
        if (!validation.IsValid)
            return ServiceResult<(Money, Guid?)>.Failure(validation.FailureReason!);

        var tempOrderId = OrderId.NewId();
        var discountAmount = discount.CalculateDiscount(orderAmount);
        discount.RecordUsage(UserId.From(userId), tempOrderId, discountAmount, dateTimeProvider.UtcNow);

        discountRepository.Update(discount);

        await auditService.LogSystemEventAsync(
            "CheckoutDiscountApplied",
            $"Discount {discountCode} applied during checkout",
            ct);

        return ServiceResult<(Money, Guid?)>.Success((discountAmount, discount.Id.Value));
    }
}