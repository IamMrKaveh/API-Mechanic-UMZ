namespace Domain.Payment.Events;

public class PaymentRefundedEvent : DomainEvent
{
    public int TransactionId { get; }
    public int OrderId { get; }
    public int UserId { get; }
    public decimal Amount { get; }
    public string? Reason { get; }

    public PaymentRefundedEvent(
        int transactionId,
        int orderId,
        int userId,
        decimal amount,
        string reason)
    {
        TransactionId = transactionId;
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
        Reason = reason;
    }
}