namespace Domain.Payment.Events;

public sealed class PaymentSucceededEvent(
    Guid transactionId,
    Guid orderId,
    long refId,
    int userId = 0,
    decimal amount = 0) : DomainEvent
{
    public Guid TransactionId { get; } = transactionId;
    public Guid OrderId { get; } = orderId;
    public long RefId { get; } = refId;
    public int UserId { get; } = userId;
    public decimal Amount { get; } = amount;
}