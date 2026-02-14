namespace Domain.Payment.Events;

public class PaymentRefundedEvent : DomainEvent
{
    public int TransactionId { get; }
    public int OrderId { get; }
    public decimal Amount { get; }

    public PaymentRefundedEvent(int transactionId, int orderId, decimal amount)
    {
        TransactionId = transactionId;
        OrderId = orderId;
        Amount = amount;
    }
}