using Application.Search.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Search.Features.Queries.SearchProducts;

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
public sealed record SearchProductsQuery(
    string? Q,
    Guid? CategoryId,
    Guid? BrandId,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool InStockOnly,
    string? SortBy,
    int Page = 1,
    int PageSize = 10)
    : IQuery<SearchResultDto<ProductSearchResultItemDto>>;