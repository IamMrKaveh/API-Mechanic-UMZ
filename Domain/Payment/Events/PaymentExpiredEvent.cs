namespace Domain.Payment.Events;

public class PaymentExpiredEvent : DomainEvent
{
    public int TransactionId { get; }
    public int OrderId { get; }
    public decimal Amount { get; }
    public string Authority { get; }

    public PaymentExpiredEvent(int transactionId, int orderId, decimal amount, string authority)
    {
        TransactionId = transactionId;
        OrderId = orderId;
        Amount = amount;
        Authority = authority;
    }
}