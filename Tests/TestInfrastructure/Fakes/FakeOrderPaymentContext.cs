using Domain.Order.ValueObjects;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Tests.TestInfrastructure.Fakes;

public sealed class FakeOrderPaymentContext : IOrderPaymentContext
{
    public OrderId Id { get; set; } = OrderId.NewId();
    public bool IsPaid { get; set; }
    public bool IsDelivered { get; set; }
    public string StatusDisplayName { get; set; } = "Created";

    public int RefundCallCount { get; private set; }
    public int MarkAsPaidCallCount { get; private set; }
    public int StartProcessingCallCount { get; private set; }
    public PaymentTransactionId? MarkAsPaidWithTransactionId { get; private set; }

    public void Refund()
    {
        RefundCallCount++;
    }

    public void MarkAsPaid(PaymentTransactionId paymentTransactionId)
    {
        MarkAsPaidCallCount++;
        MarkAsPaidWithTransactionId = paymentTransactionId;
        IsPaid = true;
    }

    public void StartProcessing()
    {
        StartProcessingCallCount++;
        StatusDisplayName = "Processing";
    }
}
