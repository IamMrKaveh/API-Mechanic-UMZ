namespace Domain.Payment.Events;

public sealed class PaymentInitiatedEvent(Guid transactionId, Guid orderId, decimal amount) : DomainEvent
{
    public Guid TransactionId { get; } = transactionId;
    public Guid OrderId { get; } = orderId;
    public decimal Amount { get; } = amount;
}