namespace Domain.Payment.Events;

public sealed class PaymentExpiredEvent(Guid transactionId, Guid orderId, decimal amount, string authority) : DomainEvent
{
    public Guid TransactionId { get; } = transactionId;
    public Guid OrderId { get; } = orderId;
    public decimal Amount { get; } = amount;
    public string Authority { get; } = authority;
}