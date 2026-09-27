using Application.Payment.Features.Adapters;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Interfaces;
using Domain.Payment.Services;

namespace Application.Payment.Features.Commands.AtomicRefundPayment;

public class AtomicRefundPaymentHandler(
    IOrderRepository orderRepository,
    IPaymentTransactionRepository paymentRepository)
    : ICommandHandler<AtomicRefundPaymentCommand>
{
    public async Task<ServiceResult> Handle(AtomicRefundPaymentCommand request, CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var orderResult = await (orderRepository.FindByIdAsync(orderId, ct)).OrNotFoundAsync("سفارش یافت نشد.");
        if (orderResult.IsFailure) return orderResult.Error;
        var order = orderResult.Value;

        if (!order.IsPaid)
            return ServiceResult.Failure("سفارش پرداخت نشده است.");

        var paymentResult = await (paymentRepository.GetVerifiedByOrderIdAsync(orderId, ct)).OrNotFoundAsync("تراکنش پرداخت یافت نشد.");
        if (paymentResult.IsFailure) return paymentResult.Error;
        var payment = paymentResult.Value;

        var eligibility = PaymentSettlementService.ValidateRefundEligibility(
            new OrderPaymentContextAdapter(order), payment);

        if (!eligibility.IsValid)
            return ServiceResult.Failure(eligibility.Error!);

        var refundResult = PaymentSettlementService.ProcessRefund(
            new OrderPaymentContextAdapter(order), payment, request.Reason);

        if (!refundResult.IsSuccess)
            return ServiceResult.Failure(refundResult.Error!);

        orderRepository.Update(order);
        paymentRepository.Update(payment);

        return ServiceResult.Success();
    }
}