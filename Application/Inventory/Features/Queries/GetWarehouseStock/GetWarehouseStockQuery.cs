using Application.Inventory.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Inventory.Features.Queries.GetWarehouseStock;

[RequestOutputCache(
    tags:
    [
        CacheTags.Inventory,
        CacheTags.ProductVariant,
        CacheTags.Warehouse
    ],
    expirationInSeconds: 15)]
public record GetWarehouseStockQuery(Guid VariantId)
    : IQuery<IEnumerable<WarehouseStockDto>>;