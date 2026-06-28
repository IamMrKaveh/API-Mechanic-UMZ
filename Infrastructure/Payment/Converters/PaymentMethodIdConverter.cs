using Domain.Payment.ValueObjects;

namespace Infrastructure.Payment.Converters;

internal sealed class PaymentMethodIdConverter : StronglyTypedIdConverter<PaymentMethodId>
{
    public PaymentMethodIdConverter() : base(PaymentMethodId.From)
    {
    }
}