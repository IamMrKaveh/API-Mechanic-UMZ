namespace Domain.Payment.Events;

public sealed class PaymentCancelledEvent : DomainEvent
{
    public int TransactionId { get; }
    public int OrderId { get; }
    public string? Reason { get; }

    public PaymentCancelledEvent(int transactionId, int orderId, string? reason)
    {
        TransactionId = transactionId;
        OrderId = orderId;
        Reason = reason;
    }
}