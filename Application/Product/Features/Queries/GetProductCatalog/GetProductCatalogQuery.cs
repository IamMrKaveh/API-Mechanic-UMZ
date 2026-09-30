using Application.Cache.Contracts;
using Application.Product.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Product.Features.Queries.GetProductCatalog;

[RequestOutputCache(
    tags:
    [
        CacheTags.Product,
        CacheTags.ProductVariant,
        CacheTags.Category,
        CacheTags.Brand,
        CacheTags.Inventory,
        CacheTags.Media
    ],
    expirationInSeconds: 60)]
public record GetProductCatalogQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool InStockOnly = false,
    string? SortBy = null,
    bool? IsFeatured = null,
    bool? HasDiscount = null)
    : IPageQuery<ProductCatalogItemDto>;
