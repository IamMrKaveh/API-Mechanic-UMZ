using Application.Inventory.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Inventory.Features.Queries.GetInventoryStatistics;

[RequestOutputCache(
    tags:
    [
        CacheTags.Inventory,
        CacheTags.ProductVariant,
        CacheTags.Warehouse
    ],
    expirationInSeconds: 30)]
public record GetInventoryStatisticsQuery : IQuery<InventoryStatisticsDto>;