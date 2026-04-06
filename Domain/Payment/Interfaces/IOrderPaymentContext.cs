using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;

namespace Domain.Payment.Interfaces;

public interface IOrderPaymentContext
{
    OrderId Id { get; }
    bool IsPaid { get; }
    bool IsDelivered { get; }
    string StatusDisplayName { get; }

    void Refund();

    void MarkAsPaid(PaymentTransactionId paymentTransactionId);

    void StartProcessing();
}