using Domain.Order.ValueObjects;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Application.Payment.Features.Adapters;

internal sealed class OrderPaymentContextAdapter(Domain.Order.Aggregates.Order order)
    : IOrderPaymentContext
{
    public OrderId Id => order.Id;
    public bool IsPaid => order.IsPaid;
    public bool IsDelivered => order.IsDelivered;
    public string StatusDisplayName => order.Status.DisplayName;

    public void Refund() => order.Refund();

    public void MarkAsPaid(PaymentTransactionId paymentTransactionId)
        => order.MarkAsPaid(paymentTransactionId);

    public void StartProcessing() => order.StartProcessing();
}