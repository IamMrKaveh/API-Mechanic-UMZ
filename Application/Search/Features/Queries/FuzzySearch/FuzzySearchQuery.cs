using Application.Search.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Search.Features.Queries.FuzzySearch;

[RequestOutputCache(
    tags:
    [
        CacheTags.Product,
        CacheTags.ProductVariant,
        CacheTags.Category,
        CacheTags.Brand,
        CacheTags.Inventory
    ],
    expirationInSeconds: 60)]
public record FuzzySearchQuery(
    string Q,
    int Page = 1,
    int PageSize = 10)
    : IQuery<SearchResultDto<ProductSearchResultItemDto>>;