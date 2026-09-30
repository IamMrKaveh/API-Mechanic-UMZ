using Application.Payment.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Payment.Features.Queries.GetActivePaymentMethods;

[RequestOutputCache(
    tags:
    [
        CacheTags.PaymentMethod
    ],
    expirationInSeconds: 300)]
public record GetActivePaymentMethodsQuery(decimal OrderAmount = 0m)
    : IQuery<IReadOnlyList<AvailablePaymentMethodDto>>;