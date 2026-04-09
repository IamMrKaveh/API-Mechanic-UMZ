using Application.Payment.Features.Shared;
using Domain.Order.Interfaces;

namespace Application.Payment.Features.Commands.InitiatePayment;

public class InitiatePaymentHandler(
    IOrderRepository orderRepository,
    IPaymentService paymentService,
    ILogger<InitiatePaymentHandler> logger) : IRequestHandler<InitiatePaymentCommand, ServiceResult<PaymentInitiationResult>>
{
    public async Task<ServiceResult<PaymentInitiationResult>> Handle(
        InitiatePaymentCommand request, CancellationToken ct)
    {
        var order = await orderRepository.FindByIdAsync(request.OrderId, ct);
        if (order is null)
            return ServiceResult<PaymentInitiationResult>.NotFound("سفارش یافت نشد.");

        if (order.UserId != request.UserId)
            return ServiceResult<PaymentInitiationResult>.Forbidden("دسترسی ممنوع.");

        if (order.IsPaid)
            return ServiceResult<PaymentInitiationResult>.Conflict("سفارش قبلاً پرداخت شده است.");

        return await paymentService.InitiatePaymentAsync(
            request.OrderId, order.FinalAmount.Amount, request.IpAddress, ct);
    }
}