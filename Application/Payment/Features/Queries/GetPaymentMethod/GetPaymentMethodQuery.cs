using Application.Payment.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Payment.Features.Queries.GetPaymentMethod;

[RequestOutputCache(
    tags:
    [
        CacheTags.PaymentMethod
    ],
    expirationInSeconds: 300)]
public record GetPaymentMethodQuery(Guid Id) : IQuery<PaymentMethodDto>;