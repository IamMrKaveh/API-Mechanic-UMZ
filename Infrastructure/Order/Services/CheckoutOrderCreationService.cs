using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Order.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Infrastructure.Order.Services;

public class CheckoutOrderCreationService(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : ICheckoutOrderCreationService
{
    public async Task<ServiceResult<CheckoutResultDto>> CreateAsync(
        Guid userId,
        ReceiverInfo receiverInfo,
        DeliveryAddress deliveryAddress,
        List<OrderItemSnapshot> items,
        Money shippingCost,
        Money discountAmount,
        Guid? discountCodeId,
        Guid idempotencyKey,
        CancellationToken ct)
    {
        if (await orderRepository.ExistsByIdempotencyKeyAsync(idempotencyKey, ct))
            return ServiceResult<CheckoutResultDto>.Conflict("سفارش قبلاً ثبت شده است.");

        var orderId = OrderId.NewId();
        var order = Order.Place(
            orderId,
            userId,
            receiverInfo,
            deliveryAddress,
            shippingCost,
            discountAmount,
            discountCodeId,
            items,
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