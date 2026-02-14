namespace Domain.Payment.Events;

public class PaymentInitiatedEvent : DomainEvent
{
    public int TransactionId { get; }
    public int OrderId { get; }
    public decimal Amount { get; }

    public PaymentInitiatedEvent(int transactionId, int orderId, decimal amount)
    {
        TransactionId = transactionId;
        OrderId = orderId;
        Amount = amount;
    }
}