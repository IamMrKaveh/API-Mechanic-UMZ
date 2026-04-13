using Application.Payment.Features.Adapters;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Interfaces;
using Domain.Payment.Services;

namespace Application.Payment.Features.Commands.AtomicRefundPayment;

public class AtomicRefundPaymentHandler(
    IOrderRepository orderRepository,
    IPaymentTransactionRepository paymentRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<AtomicRefundPaymentCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(AtomicRefundPaymentCommand request, CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (!order.IsPaid)
            return ServiceResult.Failure("سفارش پرداخت نشده است.");

        var payment = await paymentRepository.GetVerifiedByOrderIdAsync(orderId, ct);
        if (payment is null)
            return ServiceResult.NotFound("تراکنش پرداخت یافت نشد.");

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
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}