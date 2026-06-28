using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetPaymentMethod;

public record GetPaymentMethodQuery(Guid Id) : IQuery<PaymentMethodDto>;