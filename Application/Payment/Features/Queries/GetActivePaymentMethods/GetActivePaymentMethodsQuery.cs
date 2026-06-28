using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetActivePaymentMethods;

public record GetActivePaymentMethodsQuery(decimal OrderAmount = 0m)
    : IQuery<IReadOnlyList<AvailablePaymentMethodDto>>;