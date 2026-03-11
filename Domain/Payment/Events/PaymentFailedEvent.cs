namespace Domain.Payment.Events;

public sealed class PaymentFailedEvent(Guid transactionId, Guid orderId, string reason) : DomainEvent
{
    public Guid TransactionId { get; } = transactionId;
    public Guid OrderId { get; } = orderId;
    public string Reason { get; } = reason;
}