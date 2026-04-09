using Domain.Order.Interfaces;
using Domain.Payment.Interfaces;
using Domain.Payment.Services;

namespace Application.Payment.Features.Commands.AtomicRefundPayment;

public class AtomicRefundPaymentHandler(
    IOrderRepository orderRepository,
    IPaymentTransactionRepository paymentRepository,
    IUnitOfWork unitOfWork,
    ILogger<AtomicRefundPaymentHandler> logger) : IRequestHandler<AtomicRefundPaymentCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(AtomicRefundPaymentCommand request, CancellationToken ct)
    {
        var order = await orderRepository.FindByIdAsync(request.OrderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (!order.IsPaid)
            return ServiceResult.Failure("سفارش پرداخت نشده است.");

        var payment = await paymentRepository.GetVerifiedByOrderIdAsync(request.OrderId, ct);
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

        logger.LogInformation("Refund processed for order {OrderId}", request.OrderId);
        return ServiceResult.Success();
    }
}

internal sealed class OrderPaymentContextAdapter(Domain.Order.Aggregates.Order order)
    : Domain.Payment.Interfaces.IOrderPaymentContext
{
    public Guid Id => order.Id.Value;
    public bool IsPaid => order.IsPaid;
    public bool IsDelivered => order.IsDelivered;
    public string StatusDisplayName => order.Status.DisplayName;

    public void Refund() => order.Refund();

    public void MarkAsPaid(Guid paymentTransactionId) => order.MarkAsPaid(paymentTransactionId);

    public void StartProcessing() => order.StartProcessing();
}