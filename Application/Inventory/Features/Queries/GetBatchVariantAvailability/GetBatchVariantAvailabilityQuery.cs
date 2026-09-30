using Application.Inventory.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Inventory.Features.Queries.GetBatchVariantAvailability;

[RequestOutputCache(
    tags:
    [
        CacheTags.Inventory,
        CacheTags.ProductVariant
    ],
    expirationInSeconds: 15)]
public record GetBatchVariantAvailabilityQuery(
    ICollection<Guid> VariantIds)
    : IQuery<IReadOnlyList<VariantAvailabilityDto>>;