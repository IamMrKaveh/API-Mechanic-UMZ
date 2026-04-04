namespace Domain.Payment.Interfaces;

public interface IOrderPaymentContext
{
    Guid Id { get; }
    bool IsPaid { get; }
    bool IsDelivered { get; }
    string StatusDisplayName { get; }

    void Refund();

    void MarkAsPaid(Guid paymentTransactionId);

    void StartProcessing();
}