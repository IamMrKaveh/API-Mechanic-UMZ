using Application.Common.Results;
using Application.Order.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutOrderCreationService
{
    Task<ServiceResult<CheckoutResultDto>> CreateAsync(
        Guid userId,
        ReceiverInfo receiverInfo,
        DeliveryAddress deliveryAddress,
        List<OrderItemSnapshot> items,
        Money shippingCost,
        Money discountAmount,
        Guid? discountCodeId,
        Guid idempotencyKey,
        CancellationToken ct);
}