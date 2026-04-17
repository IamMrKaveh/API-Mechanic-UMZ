using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Order.Features.Shared;
using Domain.Discount.ValueObjects;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Order.Services;

public sealed class CheckoutOrderCreationService(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : ICheckoutOrderCreationService
{
    public async Task<ServiceResult<CheckoutResultDto>> CreateAsync(
        Guid userId,
        ReceiverInfo receiverInfo,
        DeliveryAddress deliveryAddress,
        IReadOnlyCollection<OrderItemSnapshot> items,
        Money shippingCost,
        Money discountAmount,
        Guid? discountCodeId,
        Guid idempotencyKey,
        CancellationToken ct)
    {
        if (await orderRepository.ExistsByIdempotencyKeyAsync(idempotencyKey, ct))
            return ServiceResult<CheckoutResultDto>.Conflict("سفارش قبلاً ثبت شده است.");

        var orderId = OrderId.NewId();
        var order = Domain.Order.Aggregates.Order.Place(
            orderId,
            UserId.From(userId),
            receiverInfo,
            deliveryAddress,
            shippingCost,
            discountAmount,
            DiscountCodeId.From(discountCodeId.Value),
            items.ToList(),
            idempotencyKey);

        orderRepository.Add(order);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<CheckoutResultDto>.Success(new CheckoutResultDto
        {
            OrderId = orderId.Value,
            OrderNumber = order.OrderNumber.Value,
            FinalAmount = order.FinalAmount.Amount
        });
    }
}