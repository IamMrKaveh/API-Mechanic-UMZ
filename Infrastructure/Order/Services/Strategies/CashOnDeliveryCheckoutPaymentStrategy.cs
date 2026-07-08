using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Order.Features.Shared;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Order.Services.Strategies;

public sealed class CashOnDeliveryCheckoutPaymentStrategy(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : ICheckoutPaymentStrategy
{
    public string Code => "cash_on_delivery";

    public async Task<ServiceResult<CheckoutResultDto>> ExecuteAsync(
        CheckoutResultDto orderResult,
        OrderId orderId,
        UserId userId,
        Money finalAmount,
        string ipAddress,
        string? userAgent,
        Guid idempotencyKey,
        CancellationToken ct)
    {
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult<CheckoutResultDto>.NotFound("سفارش یافت نشد.");

        if (order.Status == OrderStatusValue.Created)
            order.MoveToPending();

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<CheckoutResultDto>.Success(orderResult with
        {
            PaymentUrl = null,
            PaymentAuthority = null,
            PaymentTransactionId = null,
            IsPaid = false,
            PaymentMethodCode = Code
        });
    }
}