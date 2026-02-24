namespace Domain.Payment.Events;

public class PaymentFailedEvent : DomainEvent
{
    public int TransactionId { get; }
    public int OrderId { get; }
    public string Reason { get; }

    public PaymentFailedEvent(int transactionId, int orderId, string reason)
    {
        TransactionId = transactionId;
        OrderId = orderId;
        Reason = reason;
    }
}