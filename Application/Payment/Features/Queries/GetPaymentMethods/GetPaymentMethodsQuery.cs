using Application.Payment.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Payment.Features.Queries.GetPaymentMethods;

[RequestOutputCache(
    tags:
    [
        CacheTags.PaymentMethod
    ],
    expirationInSeconds: 1800)]
public record GetPaymentMethodsQuery(
    bool IncludeInactive = false,
    bool IncludeDeleted = false)
    : IQuery<IReadOnlyList<PaymentMethodListItemDto>>;
