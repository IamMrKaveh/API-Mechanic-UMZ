using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Discount.Contracts;

public interface IDiscountService
{
    Task<ServiceResult> ApplyDiscountAsync(
    string code,
    Money orderAmount,
    UserId userId,
    OrderId orderId,
    CancellationToken ct = default);

    Task<ServiceResult> CancelDiscountUsageAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task<ServiceResult> ConfirmDiscountUsageAsync(
        OrderId orderId,
        CancellationToken ct = default);
}