namespace Domain.Payment.Events;

public class PaymentRefundedEvent : DomainEvent
{
    public int TransactionId { get; }
    public int OrderId { get; }
    public decimal Amount { get; }
    public string? Reason { get; }

    public PaymentRefundedEvent(
        int transactionId,
        int orderId,
        decimal amount,
        string reason)
    {
        TransactionId = transactionId;
        OrderId = orderId;
        Amount = amount;
        Reason = reason;
    }
}