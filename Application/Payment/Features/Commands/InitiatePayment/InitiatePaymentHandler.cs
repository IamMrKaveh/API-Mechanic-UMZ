using Application.Payment.Features.Shared;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Payment.Features.Commands.InitiatePayment;

public class InitiatePaymentHandler(
    IOrderRepository orderRepository,
    IPaymentService paymentService) : IRequestHandler<InitiatePaymentCommand, ServiceResult<PaymentInitiationResult>>
{
    public async Task<ServiceResult<PaymentInitiationResult>> Handle(
        InitiatePaymentCommand request, CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult<PaymentInitiationResult>.NotFound("سفارش یافت نشد.");

        if (order.UserId != UserId.From(request.UserId))
            return ServiceResult<PaymentInitiationResult>.Forbidden("دسترسی ممنوع.");

        if (order.IsPaid)
            return ServiceResult<PaymentInitiationResult>.Conflict("سفارش قبلاً پرداخت شده است.");

        return await paymentService.InitiatePaymentAsync(
            orderId,
            order.FinalAmount,
            IpAddress.Create(request.IpAddress),
            ct);
    }
}