namespace Domain.Payment.Events;

public sealed class PaymentRefundedEvent(
    Guid transactionId,
    Guid orderId,
    int userId,
    decimal amount,
    string? reason) : DomainEvent
{
    public Guid TransactionId { get; } = transactionId;
    public Guid OrderId { get; } = orderId;
    public int UserId { get; } = userId;
    public decimal Amount { get; } = amount;
    public string? Reason { get; } = reason;
}