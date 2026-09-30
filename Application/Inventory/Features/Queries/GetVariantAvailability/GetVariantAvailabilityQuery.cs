using Application.Inventory.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Inventory.Features.Queries.GetVariantAvailability;

[RequestOutputCache(
    tags:
    [
        CacheTags.Inventory,
        CacheTags.ProductVariant
    ],
    expirationInSeconds: 30)]
public record GetVariantAvailabilityQuery(Guid VariantId)
    : IQuery<VariantAvailabilityDto>;
