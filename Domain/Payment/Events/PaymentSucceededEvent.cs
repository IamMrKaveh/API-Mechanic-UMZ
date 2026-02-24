namespace Domain.Payment.Events;

public class PaymentSucceededEvent : DomainEvent
{
    public int TransactionId { get; }
    public int OrderId { get; }
    public long RefId { get; }
    public int UserId { get; }
    public string? CardPan { get; }

    public PaymentSucceededEvent(
        int transactionId,
        int orderId,
        long refId,
        int userId = 0,
        string? cardPan = null)
    {
        TransactionId = transactionId;
        OrderId = orderId;
        RefId = refId;
        UserId = userId;
        CardPan = cardPan;
    }
}