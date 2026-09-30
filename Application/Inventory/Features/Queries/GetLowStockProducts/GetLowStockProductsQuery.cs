using Application.Inventory.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Inventory.Features.Queries.GetLowStockProducts;

[RequestOutputCache(
    tags:
    [
        CacheTags.Inventory,
        CacheTags.ProductVariant,
        CacheTags.Product
    ],
    expirationInSeconds: 30)]
public record GetLowStockProductsQuery(int Threshold = 5)
    : IQuery<IEnumerable<LowStockItemDto>>;