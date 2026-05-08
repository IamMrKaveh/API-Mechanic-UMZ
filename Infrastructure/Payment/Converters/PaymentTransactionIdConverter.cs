using Domain.Payment.ValueObjects;

namespace Infrastructure.Payment.Converters;

internal sealed class PaymentTransactionIdConverter : StronglyTypedIdConverter<PaymentTransactionId>
{
    public PaymentTransactionIdConverter() : base(PaymentTransactionId.From)
    {
    }
}