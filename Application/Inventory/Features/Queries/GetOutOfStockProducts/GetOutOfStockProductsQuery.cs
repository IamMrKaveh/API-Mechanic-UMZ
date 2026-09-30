using Application.Inventory.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Inventory.Features.Queries.GetOutOfStockProducts;

[RequestOutputCache(
    tags:
    [
        CacheTags.Inventory,
        CacheTags.ProductVariant,
        CacheTags.Product
    ],
    expirationInSeconds: 30)]
public record GetOutOfStockProductsQuery() : IQuery<IEnumerable<OutOfStockItemDto>>;